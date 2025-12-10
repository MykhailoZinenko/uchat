-- uchat Database Schema - Phases 1-3
-- SQLite DDL
-- Created: 2025-11-26
-- Updated: 2025-11-30
--
-- Design Philosophy:
-- - No system user (follows Telegram/Discord pattern)
-- - Service messages use MessageType field with NULL sender
-- - Global room created by system (CreatedByUserId = NULL)

-- ============================================================================
-- PHASE 1: Core Foundation
-- ============================================================================

-- Users table
CREATE TABLE Users (
    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE COLLATE NOCASE,
    Email TEXT UNIQUE COLLATE NOCASE,
    PasswordHash TEXT NOT NULL,
    DisplayName TEXT,
    Bio TEXT,
    AvatarUrl TEXT,
    StatusText TEXT,
    IsOnline BOOLEAN NOT NULL DEFAULT 0,
    LastSeenAt DATETIME,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME
);

CREATE INDEX idx_users_username ON Users(Username);
CREATE INDEX idx_users_email ON Users(Email) WHERE Email IS NOT NULL;
CREATE INDEX idx_users_online ON Users(IsOnline) WHERE IsOnline = 1;

-- Sessions table
CREATE TABLE Sessions (
    SessionId INTEGER PRIMARY KEY AUTOINCREMENT,
    SessionToken TEXT NOT NULL UNIQUE,
    UserId INTEGER NOT NULL,
    DeviceInfo TEXT NOT NULL,
    IpAddress TEXT,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastActivityAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ExpiresAt DATETIME NOT NULL,
    IsRevoked BOOLEAN NOT NULL DEFAULT 0,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE UNIQUE INDEX idx_sessions_token ON Sessions(SessionToken);
CREATE INDEX idx_sessions_user ON Sessions(UserId) WHERE IsRevoked = 0;
CREATE INDEX idx_sessions_expires ON Sessions(ExpiresAt) WHERE IsRevoked = 0;

-- Rooms table
CREATE TABLE Rooms (
    RoomId INTEGER PRIMARY KEY AUTOINCREMENT,
    RoomType TEXT NOT NULL CHECK(RoomType IN ('global', 'direct', 'group')),
    RoomName TEXT,
    RoomDescription TEXT,
    AvatarUrl TEXT,
    IsGlobal BOOLEAN NOT NULL DEFAULT 0,
    CreatedByUserId INTEGER,  -- NULL for system-created rooms (like global chat)
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME,
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId) ON DELETE RESTRICT,
    CHECK(
        (RoomType = 'global' AND IsGlobal = 1 AND RoomName IS NOT NULL AND CreatedByUserId IS NULL) OR
        (RoomType = 'group' AND IsGlobal = 0 AND RoomName IS NOT NULL AND CreatedByUserId IS NOT NULL) OR
        (RoomType = 'direct' AND IsGlobal = 0 AND CreatedByUserId IS NOT NULL)
    )
);

CREATE INDEX idx_rooms_type ON Rooms(RoomType);
CREATE INDEX idx_rooms_creator ON Rooms(CreatedByUserId) WHERE CreatedByUserId IS NOT NULL;
CREATE UNIQUE INDEX idx_rooms_global ON Rooms(IsGlobal) WHERE IsGlobal = 1;

-- RoomMembers table
CREATE TABLE RoomMembers (
    MemberId INTEGER PRIMARY KEY AUTOINCREMENT,
    RoomId INTEGER NOT NULL,
    UserId INTEGER NOT NULL,
    MemberRole TEXT NOT NULL DEFAULT 'member' CHECK(MemberRole IN ('owner', 'admin', 'member')),
    JoinedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LeftAt DATETIME,
    LastReadMessageId INTEGER,
    IsMuted BOOLEAN NOT NULL DEFAULT 0,
    FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (LastReadMessageId) REFERENCES Messages(MessageId) ON DELETE SET NULL,
    UNIQUE(RoomId, UserId)
);

CREATE INDEX idx_members_room ON RoomMembers(RoomId) WHERE LeftAt IS NULL;
CREATE INDEX idx_members_user ON RoomMembers(UserId) WHERE LeftAt IS NULL;
CREATE INDEX idx_members_role ON RoomMembers(MemberRole);

-- Messages table
CREATE TABLE Messages (
    MessageId INTEGER PRIMARY KEY AUTOINCREMENT,
    RoomId INTEGER NOT NULL,
    SenderUserId INTEGER,  -- NULL for service messages
    MessageType TEXT NOT NULL DEFAULT 'text' CHECK(MessageType IN ('text', 'service')),
    ServiceAction TEXT,  -- For service messages: 'user_joined', 'user_left', 'room_created', 'photo_changed', etc.
    ReplyToMessageId INTEGER,
    ForwardedFromMessageId INTEGER,
    Content TEXT NOT NULL,
    SentAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId) ON DELETE CASCADE,
    FOREIGN KEY (SenderUserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (ReplyToMessageId) REFERENCES Messages(MessageId) ON DELETE SET NULL,
    FOREIGN KEY (ForwardedFromMessageId) REFERENCES Messages(MessageId) ON DELETE SET NULL,
    CHECK(
        (MessageType = 'text' AND SenderUserId IS NOT NULL AND ServiceAction IS NULL) OR
        (MessageType = 'service' AND ServiceAction IS NOT NULL)
    )
);

CREATE INDEX idx_messages_room_sent ON Messages(RoomId, SentAt DESC);
CREATE INDEX idx_messages_sender ON Messages(SenderUserId) WHERE SenderUserId IS NOT NULL;
CREATE INDEX idx_messages_type ON Messages(MessageType);
CREATE INDEX idx_messages_reply ON Messages(ReplyToMessageId) WHERE ReplyToMessageId IS NOT NULL;
CREATE INDEX idx_messages_forward ON Messages(ForwardedFromMessageId) WHERE ForwardedFromMessageId IS NOT NULL;

-- ============================================================================
-- PHASE 2: Core Chat Features
-- ============================================================================

-- MessageEdits table
CREATE TABLE MessageEdits (
    EditId INTEGER PRIMARY KEY AUTOINCREMENT,
    MessageId INTEGER NOT NULL,
    EditedByUserId INTEGER NOT NULL,
    OldContent TEXT NOT NULL,
    NewContent TEXT NOT NULL,
    EditedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MessageId) REFERENCES Messages(MessageId) ON DELETE CASCADE,
    FOREIGN KEY (EditedByUserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE INDEX idx_edits_message_time ON MessageEdits(MessageId, EditedAt DESC);

-- MessageDeletions table (delete for everyone)
CREATE TABLE MessageDeletions (
    DeletionId INTEGER PRIMARY KEY AUTOINCREMENT,
    MessageId INTEGER NOT NULL UNIQUE,
    DeletedByUserId INTEGER NOT NULL,
    DeletedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MessageId) REFERENCES Messages(MessageId) ON DELETE CASCADE,
    FOREIGN KEY (DeletedByUserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE INDEX idx_deletions_deleted_by ON MessageDeletions(DeletedByUserId);

-- RoomPins table (pin for everyone in room)
CREATE TABLE RoomPins (
    PinId INTEGER PRIMARY KEY AUTOINCREMENT,
    RoomId INTEGER NOT NULL,
    MessageId INTEGER NOT NULL,
    PinnedByUserId INTEGER NOT NULL,
    PinnedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId) ON DELETE CASCADE,
    FOREIGN KEY (MessageId) REFERENCES Messages(MessageId) ON DELETE CASCADE,
    FOREIGN KEY (PinnedByUserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    UNIQUE(RoomId, MessageId)
);

CREATE INDEX idx_pins_room_time ON RoomPins(RoomId, PinnedAt DESC);
CREATE INDEX idx_pins_message ON RoomPins(MessageId);

-- Friendships table
CREATE TABLE Friendships (
    FriendshipId INTEGER PRIMARY KEY AUTOINCREMENT,
    User1Id INTEGER NOT NULL,
    User2Id INTEGER NOT NULL,
    Status TEXT NOT NULL DEFAULT 'pending' CHECK(Status IN ('pending', 'accepted', 'rejected')),
    InitiatedByUserId INTEGER NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    AcceptedAt DATETIME,
    FOREIGN KEY (User1Id) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (User2Id) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (InitiatedByUserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CHECK(User1Id < User2Id),
    UNIQUE(User1Id, User2Id)
);

CREATE INDEX idx_friendships_user1 ON Friendships(User1Id);
CREATE INDEX idx_friendships_user2 ON Friendships(User2Id);
CREATE INDEX idx_friendships_status ON Friendships(Status);

-- BlockedUsers table
CREATE TABLE BlockedUsers (
    BlockId INTEGER PRIMARY KEY AUTOINCREMENT,
    BlockerUserId INTEGER NOT NULL,
    BlockedUserId INTEGER NOT NULL,
    BlockedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (BlockerUserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (BlockedUserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CHECK(BlockerUserId != BlockedUserId),
    UNIQUE(BlockerUserId, BlockedUserId)
);

CREATE INDEX idx_blocks_blocker ON BlockedUsers(BlockerUserId);
CREATE INDEX idx_blocks_blocked ON BlockedUsers(BlockedUserId);

-- ============================================================================
-- FULL-TEXT SEARCH (Fix #8)
-- ============================================================================

-- Messages Full-Text Search
CREATE VIRTUAL TABLE MessagesSearch USING fts5(
    MessageId UNINDEXED,
    Content,
    content='Messages',
    content_rowid='MessageId'
);

-- Triggers to keep FTS in sync with Messages table
CREATE TRIGGER messages_fts_insert AFTER INSERT ON Messages
WHEN NEW.MessageType = 'text'
BEGIN
    INSERT INTO MessagesSearch(MessageId, Content) VALUES (NEW.MessageId, NEW.Content);
END;

CREATE TRIGGER messages_fts_delete AFTER DELETE ON Messages
WHEN OLD.MessageType = 'text'
BEGIN
    DELETE FROM MessagesSearch WHERE MessageId = OLD.MessageId;
END;

CREATE TRIGGER messages_fts_update AFTER UPDATE ON Messages
WHEN NEW.MessageType = 'text'
BEGIN
    DELETE FROM MessagesSearch WHERE MessageId = OLD.MessageId;
    INSERT INTO MessagesSearch(MessageId, Content) VALUES (NEW.MessageId, NEW.Content);
END;

-- ============================================================================
-- UPDATE TIMESTAMP TRIGGERS (Fix #7)
-- ============================================================================

-- Users UpdatedAt trigger
CREATE TRIGGER update_users_timestamp
AFTER UPDATE ON Users
WHEN NEW.UpdatedAt IS NULL OR NEW.UpdatedAt = OLD.UpdatedAt
BEGIN
    UPDATE Users SET UpdatedAt = CURRENT_TIMESTAMP WHERE UserId = NEW.UserId;
END;

-- Rooms UpdatedAt trigger
CREATE TRIGGER update_rooms_timestamp
AFTER UPDATE ON Rooms
WHEN NEW.UpdatedAt IS NULL OR NEW.UpdatedAt = OLD.UpdatedAt
BEGIN
    UPDATE Rooms SET UpdatedAt = CURRENT_TIMESTAMP WHERE RoomId = NEW.RoomId;
END;

-- ============================================================================
-- SYSTEM INITIALIZATION DATA
-- ============================================================================

-- Create global chat room (no creator - system initialized)
INSERT INTO Rooms (RoomId, RoomType, RoomName, RoomDescription, IsGlobal, CreatedByUserId)
VALUES (
    1,
    'global',
    'Global Chat',
    'Welcome to uchat! This is the global chat room where everyone can communicate.',
    1,
    NULL  -- System-created, no user creator
);
