using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using System.IO;

namespace Shared.Networking
{
    public static class UPnPService
    {
        private struct UPnPServiceInfo
        {
            public string ControlUrl;
            public string ServiceType;
        }

        private static UPnPServiceInfo _serviceInfo;

        public static async void OpenPort(int port)
        {
            Debug.Log($"[UPnP] Attempting to open port {port}...");
            
            try
            {
                if (string.IsNullOrEmpty(_serviceInfo.ControlUrl))
                {
                    _serviceInfo = await DiscoverGatewayAsync();
                    if (string.IsNullOrEmpty(_serviceInfo.ControlUrl))
                    {
                        Debug.LogWarning("[UPnP] No Gateway found via SSDP.");
                        return;
                    }
                }

                // Smart Delete: Find who owns the port and kill it
                await DeleteByForceAsync(port, "UDP");
                await Task.Delay(500);

                await AddPortMappingAsync(port, "UDP");

                Debug.Log($"[UPnP] Process Complete for Port {port}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UPnP] Failed: {ex.Message}");
            }
        }

        private static async Task DeleteByForceAsync(int port, string protocol)
        {
             Debug.Log($"[UPnP] Searching for obstructing mappings on {port}...");
             for(int i=0; i<50; i++) 
             {
                 try
                 {
                    var mapping = await GetGenericPortMappingEntryAsync(i);
                    if (mapping == null) break; 

                    // Ensure we delete ANY rule on this port (TCP or UDP) to clear specific conflicts
                    if (mapping.ExternalPort == port) 
                    {
                        Debug.Log($"[UPnP] Obstructing Rule Found (Index {i}): {mapping.Protocol} {mapping.RemoteHost}:{mapping.ExternalPort} -> {mapping.InternalClient}");
                        // Delete this specific rule
                        await DeletePortMappingAsync(port, mapping.Protocol, mapping.RemoteHost);
                    }
                 }
                 catch (Exception ex)
                 {
                     Debug.LogWarning($"[UPnP] Error inspecting index {i}: {ex.Message}");
                 }
             }
        }

        private class PortMappingEntry { public string RemoteHost; public int ExternalPort; public string Protocol; public int InternalPort; public string InternalClient; }

        private static async Task<PortMappingEntry> GetGenericPortMappingEntryAsync(int index)
        {
             try 
            {
                string serviceType = _serviceInfo.ServiceType;
                string soapBody = 
                    "<?xml version=\"1.0\"?>" +
                    "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                    "<s:Body>" +
                    $"<u:GetGenericPortMappingEntry xmlns:u=\"{serviceType}\">" +
                    $"<NewPortMappingIndex>{index}</NewPortMappingIndex>" +
                    "</u:GetGenericPortMappingEntry>" +
                    "</s:Body>" +
                    "</s:Envelope>";

                 byte[] bodyBytes = Encoding.UTF8.GetBytes(soapBody);
                 HttpWebRequest req = (HttpWebRequest)WebRequest.Create(_serviceInfo.ControlUrl);
                 req.Method = "POST";
                 req.Headers.Add("SOAPACTION", $"\"{serviceType}#GetGenericPortMappingEntry\"");
                 req.ContentType = "text/xml; charset=\"utf-8\"";
                 req.ContentLength = bodyBytes.Length;

                 using (Stream reqStream = await req.GetRequestStreamAsync())
                 {
                    await reqStream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
                 }

                 using (WebResponse response = await req.GetResponseAsync()) 
                 {
                     using (var reader = new StreamReader(response.GetResponseStream()))
                     {
                        string xml = await reader.ReadToEndAsync();
                        
                        var entry = new PortMappingEntry();
                        entry.RemoteHost = ExtractXML(xml, "NewRemoteHost");
                        entry.ExternalPort = int.Parse(ExtractXML(xml, "NewExternalPort"));
                        entry.Protocol = ExtractXML(xml, "NewProtocol");
                        entry.InternalClient = ExtractXML(xml, "NewInternalClient");
                        entry.InternalPort = int.Parse(ExtractXML(xml, "NewInternalPort"));
                        return entry;
                     }
                 }
            }
            catch 
            {
                return null; 
            }
        }

        private static string ExtractXML(string xml, string tag)
        {
            // Regex to match <tag>value</tag> or <ns:tag>value</ns:tag>
            // pattern: <([a-zA-Z0-9]+:)?TAG>(.*?)</
            var match = System.Text.RegularExpressions.Regex.Match(xml, $"<([a-zA-Z0-9]+:)?{tag}>(.*?)</");
            if (match.Success) return match.Groups[2].Value;
            return "";
        }

        private static async Task GetSpecificMappingAsync(int port, string protocol)
        {
             try 
            {
                string serviceType = _serviceInfo.ServiceType;
                string soapBody = 
                    "<?xml version=\"1.0\"?>" +
                    "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                    "<s:Body>" +
                    $"<u:GetSpecificPortMappingEntry xmlns:u=\"{serviceType}\">" +
                    "<NewRemoteHost></NewRemoteHost>" +
                    $"<NewExternalPort>{port}</NewExternalPort>" +
                    $"<NewProtocol>{protocol}</NewProtocol>" +
                    "</u:GetSpecificPortMappingEntry>" +
                    "</s:Body>" +
                    "</s:Envelope>";

                 byte[] bodyBytes = Encoding.UTF8.GetBytes(soapBody);
                 HttpWebRequest req = (HttpWebRequest)WebRequest.Create(_serviceInfo.ControlUrl);
                 req.Method = "POST";
                 req.Headers.Add("SOAPACTION", $"\"{serviceType}#GetSpecificPortMappingEntry\"");
                 req.ContentType = "text/xml; charset=\"utf-8\"";
                 req.ContentLength = bodyBytes.Length;

                 using (Stream reqStream = await req.GetRequestStreamAsync())
                 {
                    await reqStream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
                 }

                 using (WebResponse response = await req.GetResponseAsync()) 
                 {
                     using (var reader = new StreamReader(response.GetResponseStream()))
                     {
                        string content = await reader.ReadToEndAsync();
                        Debug.Log($"[UPnP] Inspect {protocol} {port}: EXISTS! Details: {content}");
                     }
                 }
            }
            catch(WebException we) 
            {
                // Verify if 714 (NoSuchEntry)
                 Debug.Log($"[UPnP] Inspect {protocol} {port}: Not Found or Error ({we.Message})");
            }
        }

        private static async Task DeletePortMappingAsync(int port, string protocol, string remoteHost = "")
        {
            try 
            {
                string serviceType = _serviceInfo.ServiceType;
                string soapBody = 
                    "<?xml version=\"1.0\"?>" +
                    "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                    "<s:Body>" +
                    $"<u:DeletePortMapping xmlns:u=\"{serviceType}\">" +
                    $"<NewRemoteHost>{remoteHost}</NewRemoteHost>" +
                    $"<NewExternalPort>{port}</NewExternalPort>" +
                    $"<NewProtocol>{protocol}</NewProtocol>" +
                    "</u:DeletePortMapping>" +
                    "</s:Body>" +
                    "</s:Envelope>";

                byte[] bodyBytes = Encoding.UTF8.GetBytes(soapBody);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(_serviceInfo.ControlUrl);
                req.Method = "POST";
                req.ContentType = "text/xml; charset=\"utf-8\"";
                req.Headers.Add("SOAPACTION", $"\"{serviceType}#DeletePortMapping\"");
                req.ContentLength = bodyBytes.Length;

                using (Stream reqStream = await req.GetRequestStreamAsync())
                {
                    await reqStream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
                }

                using (WebResponse response = await req.GetResponseAsync()) { }
                Debug.Log($"[UPnP] Cleared existing mapping for {port} ({protocol})");
            }
            catch (WebException wex)
            { 
               if (wex.Response != null)
               {
                    using (var reader = new StreamReader(wex.Response.GetResponseStream()))
                    {
                        string err = await reader.ReadToEndAsync();
                        Debug.LogWarning($"[UPnP] Delete {protocol} Mapping Failed. Router Reply: {err}");
                    }
               }
               else
               {
                    Debug.LogWarning($"[UPnP] Delete {protocol} Mapping Failed: {wex.Message}");
               }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UPnP] Delete {protocol} Mapping Error: {e.Message}");
            }
        }

        private static async Task<UPnPServiceInfo> DiscoverGatewayAsync()
        {
            Debug.Log("[UPnP] Starting SSDP Discovery...");
            
            // Broaden search to 'ssdp:all' to find ANY UPnP device, then filter
            string ssdpRequest = "M-SEARCH * HTTP/1.1\r\n" +
                                 "HOST: 239.255.255.250:1900\r\n" +
                                 "ST:ssdp:all\r\n" + 
                                 "MAN:\"ssdp:discover\"\r\n" +
                                 "MX:3\r\n\r\n";

            byte[] reqBytes = Encoding.ASCII.GetBytes(ssdpRequest);
            
            // Bind to the actual Local IP to ensure we use the correct Interface (WiFi/Ethernet) and not a VM adapter
            string localIP = GetLocalIPAddress();
            IPEndPoint localEP = new IPEndPoint(IPAddress.Parse(localIP), 0);
            Debug.Log($"[UPnP] Binding UDP to {localIP}");

            using (UdpClient client = new UdpClient(localEP))
            {
                client.EnableBroadcast = true;
                client.MulticastLoopback = false;

                // Send Multicast
                try 
                {
                    await client.SendAsync(reqBytes, reqBytes.Length, "239.255.255.250", 1900);
                    Debug.Log("[UPnP] SSDP M-SEARCH Sent (ssdp:all). Waiting for responses...");
                }
                catch(Exception e)
                {
                    Debug.LogError($"[UPnP] Failed to send SSDP: {e.Message}");
                    return default;
                }

                var startTime = DateTime.Now;
                // Try for 5 seconds total
                while ((DateTime.Now - startTime).TotalSeconds < 5)
                {
                    try 
                    {
                        var receiveTask = client.ReceiveAsync();
                        var timeoutTask = Task.Delay(2000); // Wait 2s for a packet
                        
                        var completed = await Task.WhenAny(receiveTask, timeoutTask);
                        if (completed == timeoutTask)
                        {
                            Debug.Log("[UPnP] No response in this window (Timeout). Retrying loop...");
                            continue;
                        }

                        var result = await receiveTask;
                        string resp = Encoding.ASCII.GetString(result.Buffer);
                        
                        Debug.Log($"[UPnP] Received response from {result.RemoteEndPoint}");

                        // Parse Location
                        if (resp.Contains("LOCATION:"))
                        {
                            string location = GetHeaderValue(resp, "LOCATION");
                            Debug.Log($"[UPnP] Found Device Location: {location}");
                            
                            if (!string.IsNullOrEmpty(location))
                            {
                                var info = await GetControlUrlAsync(location);
                                if (!string.IsNullOrEmpty(info.ControlUrl))
                                {
                                    Debug.Log($"[UPnP] Found Service: Type={info.ServiceType}, URL={info.ControlUrl}");
                                    return info;
                                }
                            }
                        }
                    }
                    catch (Exception e) 
                    { 
                        Debug.LogWarning($"[UPnP] Receive Error: {e.Message}");
                        break; 
                    }
                }
            }
            return default;
        }

        private static string GetHeaderValue(string response, string header)
        {
            using (StringReader reader = new StringReader(response))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.ToUpper().StartsWith(header + ":"))
                    {
                        return line.Substring(header.Length + 1).Trim();
                    }
                }
            }
            return null;
        }

        private static async Task<UPnPServiceInfo> GetControlUrlAsync(string locationUrl)
        {
            try
            {
                // Download XML Description
                using (WebClient wc = new WebClient())
                {
                    string xml = await wc.DownloadStringTaskAsync(locationUrl);
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xml);

                    // Namespace manager might be needed, but simple GetElementsByTagName often works for standard schemas
                    XmlNodeList services = doc.GetElementsByTagName("service");
                    foreach (XmlNode service in services)
                    {
                        string serviceType = service["serviceType"]?.InnerText;
                        if (!string.IsNullOrEmpty(serviceType) && (serviceType.Contains("WANIPConnection") || serviceType.Contains("WANPPPConnection")))
                        {
                            string controlUrl = service["controlURL"]?.InnerText;
                            if (!string.IsNullOrEmpty(controlUrl))
                            {
                                // Handle relative URLs
                                if (!controlUrl.StartsWith("http"))
                                {
                                    Uri uri = new Uri(locationUrl);
                                    controlUrl = $"http://{uri.Host}:{uri.Port}{controlUrl}";
                                }
                                return new UPnPServiceInfo { ControlUrl = controlUrl, ServiceType = serviceType };
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UPnP] Failed to parse device XML at {locationUrl}: {e.Message}");
            }
            return default;
        }

        private static async Task AddPortMappingAsync(int port, string protocol)
        {
            string localIP = GetLocalIPAddress();
            string serviceType = _serviceInfo.ServiceType; 
            
            Debug.Log($"[UPnP] Sending SOAP Action to {_serviceInfo.ControlUrl} for {serviceType}");

            // STRICT SOAP BODY:
            // 1. Argument order matters for some routers
            // 2. NewRemoteHost should usually be empty string, but some want valid wildcard
            
            string soapBody = 
                "<?xml version=\"1.0\"?>" +
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                "<s:Body>" +
                $"<u:AddPortMapping xmlns:u=\"{serviceType}\">" +
                "<NewRemoteHost></NewRemoteHost>" +
                $"<NewExternalPort>{port}</NewExternalPort>" +
                $"<NewProtocol>{protocol}</NewProtocol>" +
                $"<NewInternalPort>{port}</NewInternalPort>" +
                $"<NewInternalClient>{localIP}</NewInternalClient>" +
                "<NewEnabled>1</NewEnabled>" +
                "<NewPortMappingDescription>TrueFighters</NewPortMappingDescription>" +
                "<NewLeaseDuration>3600</NewLeaseDuration>" +
                "</u:AddPortMapping>" +
                "</s:Body>" +
                "</s:Envelope>";
            
            // Try formatting changes if 500 continues:
            // Some routers hate headers with quotes, some require them.
            // The previous code had strict quotes which is usually correct.
            // But if 500 occurs, it might be the empty NewRemoteHost.
            
            byte[] bodyBytes = Encoding.UTF8.GetBytes(soapBody);

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(_serviceInfo.ControlUrl);
            req.Method = "POST";
            req.ContentType = "text/xml; charset=\"utf-8\"";
            req.Headers.Add("SOAPACTION", $"\"{serviceType}#AddPortMapping\"");
            req.ContentLength = bodyBytes.Length;

            try 
            {
                using (Stream reqStream = await req.GetRequestStreamAsync())
                {
                    await reqStream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
                }

                using (WebResponse response = await req.GetResponseAsync()) 
                {
                    Debug.Log($"[UPnP] Success? Response type: {response.GetType().Name}");
                }
            }
            catch(WebException wex)
            {
                if (wex.Response != null)
                {
                    using (var reader = new StreamReader(wex.Response.GetResponseStream()))
                    {
                        string err = await reader.ReadToEndAsync();
                        Debug.LogError($"[UPnP] SOAP Failed 500. Router Reply: {err}");
                    }
                }
                else
                {
                    Debug.LogError($"[UPnP] Failed: {wex.Message}");
                }
            }
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
    }
}
