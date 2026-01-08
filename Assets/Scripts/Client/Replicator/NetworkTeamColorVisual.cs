using UnityEngine;
using System.IO;
using ServerGame.Entities;
using ClientContent;

namespace Client.Replicator
{
    public class NetworkTeamColorVisual : MonoBehaviour, INetworkComponentVisual
    {
        public int TargetComponentType => (int)ComponentType.Team;

        [SerializeField] private UnityEngine.UI.Image targetImage;
        [SerializeField] private UnityEngine.UI.Graphic[] optionalGraphics; // Support multiple graphics

        private int lastTeamId = -1;

        public void OnNetworkUpdate(BinaryReader reader)
        {
            // Deserialize TeamComponent
            int teamId = reader.ReadInt32();
            bool friendlyFire = reader.ReadBoolean();

            if (teamId != lastTeamId)
            {
                // Force update even if 0 initially to confirm logic runs
                UnityEngine.Debug.Log($"[NetworkTeamColorVisual] Entity {name} Team Update: {lastTeamId} -> {teamId}");
                lastTeamId = teamId;
                ApplyTeamColor(teamId);
            }
        }

        private void ApplyTeamColor(int teamId)
        {
            ClientContent.ContentAssetRegistry.EnsureLoaded();

            var clientNet = FindFirstObjectByType<ClientNetwork>();
            if (clientNet == null) 
            {
                UnityEngine.Debug.LogWarning("[NetworkTeamColorVisual] ClientNetwork not found!");
                return;
            }
            
            if (string.IsNullOrEmpty(clientNet.CurrentGameModeId))
            {
                 UnityEngine.Debug.LogWarning("[NetworkTeamColorVisual] CurrentGameModeId is null/empty!");
                 return;
            }

            if (ContentAssetRegistry.GameModes.TryGetValue(clientNet.CurrentGameModeId, out var gm))
            {
                if (gm.teams != null && gm.teams.Length > 0)
                {
                    int index = teamId - 1;
                    if (index >= 0 && index < gm.teams.Length)
                    {
                        Color c = gm.teams[index].teamColor;
                        UnityEngine.Debug.Log($"[NetworkTeamColorVisual] Applying Team Color {c} (Team {teamId})");
                        SetColor(c);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[NetworkTeamColorVisual] Team ID {teamId} is out of range for GameMode {gm.id} (Teams: {gm.teams.Length})");
                    }
                }
                else
                {
                     UnityEngine.Debug.Log($"[NetworkTeamColorVisual] GameMode {gm.id} has no teams defined.");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[NetworkTeamColorVisual] GameMode '{clientNet.CurrentGameModeId}' not found in Registry.");
            }
        }

        private void SetColor(Color c)
        {
            if (targetImage == null) targetImage = GetComponent<UnityEngine.UI.Image>(); // Fallback

            if (targetImage != null) targetImage.color = c;
            else UnityEngine.Debug.LogWarning($"[NetworkTeamColorVisual] TargetImage is null on {name}");

            if (optionalGraphics != null)
            {
                foreach (var g in optionalGraphics)
                {
                    if (g) g.color = c;
                }
            }
        }
    }
}
