# API Documentation

## Authentication

### POST /api/auth/login
Login with email and password.

### POST /api/auth/register
Register a new user account.

## Chat

### GET /api/chat/rooms
Get all chat rooms.

### POST /api/chat/rooms
Create a new chat room.

### GET /api/chat/rooms/{roomId}/messages
Get paginated messages for a room.

### POST /api/chat/messages
Send a message.

## Users

### GET /api/user/{userId}
Get user profile.

### PUT /api/user/{userId}
Update user profile.

## SignalR Hubs

### /hubs/chat
Real-time chat hub.

### /hubs/presence
User presence hub.
