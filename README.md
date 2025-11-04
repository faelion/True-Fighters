🧩 True Figthers
📖 Serialization

This project implements a basic client–server multiplayer system in Unity, using socket-based communication (UDP).

The main flow consists of:

A Lobby Scene where the player enters their name, IP, and server port.
A Server Scene that runs the game logic (players, NPCs, projectiles, etc.).
A Client Scene that represents the player’s world and communicates with the server.

Main Scenes
Scene	Description
LobbyScene Main menu where the player inputs IP, port, and name. Managed by LobbyManager.
ServerScene	Contains server scripts (Server, ServerPlayer, ServerNPC, ServerProjectile, etc.). Loaded additively and persists during runtime.
ClientScene	Represents the player's local world. Receives updates from the server and sends input back.

🧠 Server

Manages connected players.
Handles NPCs, projectiles, and core game updates.
Sends state updates to all clients.
Receives movement/shoot inputs from clients.

👤 ServerPlayer

Stores player data (ID, position, speed, name, etc.).
Processes client inputs (movement, shooting).
Can spawn projectiles or interact with NPCs.

🤖 ServerNPC

Simulates basic NPC logic on the server.
Parameters: speed, followRange, stopRange.
Follows a player target if within range, stops if too close.
Main function: Simulate(deltaTime).

💣 ServerProjectile

Represents bullets or projectiles created by players/NPCs.
Contains position, direction, speed, and lifetime.
Updates each frame and is destroyed on collision or expiration.

🌐 NetworkConfig

Stores global network settings (host, port, player name).
Shared by Lobby, Client, and Server.

🤖 Example NPC Behavior

The server has a ServerNPC object.
The server updates the NPC:
It finds the closest ServerPlayer.
If within followRange and outside stopRange, it moves toward the player.
The updated position is sent to clients to move the NPC cube visually.

🧪 Testing Steps

Open the Lobby Scene in Unity and press Play.
Enter example values (127.0.0.1, 9050, Player1) and click Join. (Editor will play the rol of the server and client)
The open the build and enter the values (127.0.0.1, 9050, Player2). (Build will play the rol of a client)

Verify that:
The client scene loads successfully.
The NPC moves toward the player when in range.
Player can move using right click and Q to spawn a projectile.
