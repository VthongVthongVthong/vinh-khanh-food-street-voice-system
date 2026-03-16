-- =====================================================
-- Restaurant Audio Tour Platform
-- SQL Server DDL
-- =====================================================

-- -----------------------------------------------------
-- Tạo database
-- -----------------------------------------------------
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'RestaurantAudioTour')
    CREATE DATABASE RestaurantAudioTour;
GO

USE RestaurantAudioTour;
GO

-- -----------------------------------------------------
-- DROP (chạy trước khi tạo lại từ đầu)
-- Thứ tự: bảng con trước, bảng cha sau
-- -----------------------------------------------------
IF OBJECT_ID('AudioPlayLog',      'U') IS NOT NULL DROP TABLE [AudioPlayLog];
IF OBJECT_ID('VisitLog',          'U') IS NOT NULL DROP TABLE [VisitLog];
IF OBJECT_ID('RestaurantAudio',   'U') IS NOT NULL DROP TABLE [RestaurantAudio];
IF OBJECT_ID('RestaurantContent', 'U') IS NOT NULL DROP TABLE [RestaurantContent];
IF OBJECT_ID('RestaurantImage',   'U') IS NOT NULL DROP TABLE [RestaurantImage];
IF OBJECT_ID('Restaurant',        'U') IS NOT NULL DROP TABLE [Restaurant];
IF OBJECT_ID('Language',          'U') IS NOT NULL DROP TABLE [Language];
IF OBJECT_ID('User',              'U') IS NOT NULL DROP TABLE [User];
GO

CREATE TABLE [User] (
    id            INT           IDENTITY(1,1) PRIMARY KEY,
    username      NVARCHAR(100) NOT NULL UNIQUE,
    passwordHash  NVARCHAR(255) NOT NULL,
    email         NVARCHAR(255) NULL UNIQUE,
    phone         NVARCHAR(20)  NULL,
    role          NVARCHAR(20)  NOT NULL CONSTRAINT CHK_User_role CHECK (role IN ('ADMIN', 'OWNER', 'CUSTOMER')),
    createdAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    updatedAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE [Restaurant] (
    id                  INT            IDENTITY(1,1) PRIMARY KEY,
    name                NVARCHAR(255)  NOT NULL,
    address             NVARCHAR(500)  NULL,
    phone               NVARCHAR(20)   NULL,
    description         NVARCHAR(MAX)  NULL,
    latitude            FLOAT          NULL,
    longitude           FLOAT          NULL,
    triggerRadiusMeters INT            NOT NULL DEFAULT 20,
    mapLink             NVARCHAR(1000) NULL,
    createdAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    updatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    isDeleted           BIT            NOT NULL DEFAULT 0,
    ownerId             INT            NOT NULL,

    CONSTRAINT FK_Restaurant_User FOREIGN KEY (ownerId) REFERENCES [User](id)
);

CREATE INDEX IX_Restaurant_LatLng ON [Restaurant] (latitude, longitude);

CREATE TABLE [RestaurantImage] (
    id           INT            IDENTITY(1,1) PRIMARY KEY,
    imageUrl     NVARCHAR(1000) NOT NULL,
    caption      NVARCHAR(500)  NULL,
    imageType    NVARCHAR(20)   NULL CONSTRAINT CHK_RestaurantImage_type CHECK (imageType IN ('avatar', 'banner', 'gallery')),
    sortOrder    INT            NOT NULL DEFAULT 0,
    restaurantId INT            NOT NULL,

    CONSTRAINT FK_RestaurantImage_Restaurant FOREIGN KEY (restaurantId) REFERENCES [Restaurant](id)
);

CREATE TABLE [Language] (
    id   INT          IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(10) NOT NULL UNIQUE,
    name NVARCHAR(100) NOT NULL
);

CREATE TABLE [RestaurantContent] (
    id           INT           IDENTITY(1,1) PRIMARY KEY,
    title        NVARCHAR(500) NULL,
    description  NVARCHAR(MAX) NULL,
    ttsScript    NVARCHAR(MAX) NULL,
    restaurantId INT           NOT NULL,
    languageId   INT           NOT NULL,

    CONSTRAINT FK_RestaurantContent_Restaurant FOREIGN KEY (restaurantId) REFERENCES [Restaurant](id),
    CONSTRAINT FK_RestaurantContent_Language   FOREIGN KEY (languageId)   REFERENCES [Language](id),
    CONSTRAINT UQ_RestaurantContent            UNIQUE (restaurantId, languageId)
);

CREATE TABLE [RestaurantAudio] (
    id           INT            IDENTITY(1,1) PRIMARY KEY,
    audioUrl     NVARCHAR(1000) NULL,
    duration     FLOAT          NULL,
    restaurantId INT            NOT NULL,
    languageId   INT            NOT NULL,

    CONSTRAINT FK_RestaurantAudio_Restaurant FOREIGN KEY (restaurantId) REFERENCES [Restaurant](id),
    CONSTRAINT FK_RestaurantAudio_Language   FOREIGN KEY (languageId)   REFERENCES [Language](id),
    CONSTRAINT UQ_RestaurantAudio            UNIQUE (restaurantId, languageId)
);

CREATE TABLE [VisitLog] (
    id           INT       IDENTITY(1,1) PRIMARY KEY,
    visitTime    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    latitude     FLOAT     NOT NULL,
    longitude    FLOAT     NOT NULL,
    userId       INT       NOT NULL,
    restaurantId INT       NOT NULL,

    CONSTRAINT FK_VisitLog_User       FOREIGN KEY (userId)       REFERENCES [User](id),
    CONSTRAINT FK_VisitLog_Restaurant FOREIGN KEY (restaurantId) REFERENCES [Restaurant](id)
);

CREATE INDEX IX_VisitLog_userId       ON [VisitLog] (userId);
CREATE INDEX IX_VisitLog_restaurantId ON [VisitLog] (restaurantId);
CREATE INDEX IX_VisitLog_visitTime    ON [VisitLog] (visitTime);

CREATE TABLE [AudioPlayLog] (
    id               INT       IDENTITY(1,1) PRIMARY KEY,
    playTime         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    durationListened FLOAT     NULL,
    userId           INT       NOT NULL,
    restaurantId     INT       NOT NULL,
    languageId       INT       NOT NULL,

    CONSTRAINT FK_AudioPlayLog_User       FOREIGN KEY (userId)       REFERENCES [User](id),
    CONSTRAINT FK_AudioPlayLog_Restaurant FOREIGN KEY (restaurantId) REFERENCES [Restaurant](id),
    CONSTRAINT FK_AudioPlayLog_Language   FOREIGN KEY (languageId)   REFERENCES [Language](id)
);

CREATE INDEX IX_AudioPlayLog_restaurantId ON [AudioPlayLog] (restaurantId);
CREATE INDEX IX_AudioPlayLog_userId       ON [AudioPlayLog] (userId);
CREATE INDEX IX_AudioPlayLog_playTime     ON [AudioPlayLog] (playTime);


-- =====================================================
-- SEED DATA
-- =====================================================

-- -----------------------------------------------------
-- User
-- passwordHash = bcrypt('password123')
-- -----------------------------------------------------
SET IDENTITY_INSERT [User] ON;
INSERT INTO [User] (id, username, passwordHash, email, phone, role, createdAt, updatedAt) VALUES
(1, 'admin',       '$2b$10$abcdefghijklmnopqrstuuVfjNLMvbKDFkMnYBJKFdopBxL6HS3Iq', 'admin@example.com',   '0900000001', 'ADMIN',    '2024-01-01 00:00:00', '2024-01-01 00:00:00'),
(2, 'owner_hanoi', '$2b$10$abcdefghijklmnopqrstuuVfjNLMvbKDFkMnYBJKFdopBxL6HS3Iq', 'hanoi@example.com',   '0900000002', 'OWNER',    '2024-01-02 00:00:00', '2024-01-02 00:00:00'),
(3, 'owner_saigon','$2b$10$abcdefghijklmnopqrstuuVfjNLMvbKDFkMnYBJKFdopBxL6HS3Iq', 'saigon@example.com',  '0900000003', 'OWNER',    '2024-01-02 00:00:00', '2024-01-02 00:00:00'),
(4, 'customer_01', '$2b$10$abcdefghijklmnopqrstuuVfjNLMvbKDFkMnYBJKFdopBxL6HS3Iq', 'user1@example.com',   '0911000001', 'CUSTOMER', '2024-02-01 00:00:00', '2024-02-01 00:00:00'),
(5, 'customer_02', '$2b$10$abcdefghijklmnopqrstuuVfjNLMvbKDFkMnYBJKFdopBxL6HS3Iq', 'user2@example.com',   '0911000002', 'CUSTOMER', '2024-02-05 00:00:00', '2024-02-05 00:00:00');
SET IDENTITY_INSERT [User] OFF;

-- -----------------------------------------------------
-- Restaurant
-- -----------------------------------------------------
SET IDENTITY_INSERT [Restaurant] ON;
INSERT INTO [Restaurant] (id, name, address, phone, description, latitude, longitude, triggerRadiusMeters, mapLink, createdAt, updatedAt, isDeleted, ownerId) VALUES
(1, N'Chả Cá Lã Vọng',          N'14 Chả Cá, Hoàn Kiếm, Hà Nội',          '02438250082', N'Nhà hàng chả cá nổi tiếng hơn 100 năm tại Hà Nội.',         21.033868, 105.848151, 30, 'https://maps.app.goo.gl/abc1', '2024-01-10 00:00:00', '2024-01-10 00:00:00', 0, 2),
(2, N'Bún Chả Hương Liên',       N'24 Lê Văn Hưu, Hai Bà Trưng, Hà Nội',   '02439439777', N'Quán bún chả nổi tiếng với chuyến thăm của Obama năm 2016.', 21.021774, 105.851192, 25, 'https://maps.app.goo.gl/abc2', '2024-01-11 00:00:00', '2024-01-11 00:00:00', 0, 2),
(3, N'Phở Thìn Bờ Hồ',          N'61 Đinh Tiên Hoàng, Hoàn Kiếm, Hà Nội', '02438252853', N'Phở bò xào truyền thống nổi tiếng nhất Hà Nội.',             21.028618, 105.852398, 20, 'https://maps.app.goo.gl/abc3', '2024-01-12 00:00:00', '2024-01-12 00:00:00', 0, 2),
(4, N'Nhà Hàng Ngon',            N'160 Pasteur, Quận 3, TP.HCM',            '02838277131', N'Ẩm thực ba miền trong không gian biệt thự Pháp cổ điển.',    10.779650, 106.694820, 30, 'https://maps.app.goo.gl/abc4', '2024-01-15 00:00:00', '2024-01-15 00:00:00', 0, 3),
(5, N'Cơm Tấm Thuận Kiều',       N'199 Võ Văn Tần, Quận 3, TP.HCM',        '02838322666', N'Cơm tấm sườn bì chả đặc trưng Sài Gòn từ năm 1975.',        10.772940, 106.686510, 20, 'https://maps.app.goo.gl/abc5', '2024-01-16 00:00:00', '2024-01-16 00:00:00', 0, 3);
SET IDENTITY_INSERT [Restaurant] OFF;

-- -----------------------------------------------------
-- RestaurantImage
-- -----------------------------------------------------
SET IDENTITY_INSERT [RestaurantImage] ON;
INSERT INTO [RestaurantImage] (id, imageUrl, caption, imageType, sortOrder, restaurantId) VALUES
(1,  'https://cdn.example.com/r1-avatar.jpg',  N'Logo Chả Cá Lã Vọng',       'avatar',  0, 1),
(2,  'https://cdn.example.com/r1-banner.jpg',  N'Không gian nhà hàng',        'banner',  0, 1),
(3,  'https://cdn.example.com/r1-g1.jpg',      N'Món chả cá đặc trưng',       'gallery', 1, 1),
(4,  'https://cdn.example.com/r2-avatar.jpg',  N'Logo Bún Chả Hương Liên',    'avatar',  0, 2),
(5,  'https://cdn.example.com/r2-banner.jpg',  N'Bàn Obama từng ngồi',        'banner',  0, 2),
(6,  'https://cdn.example.com/r3-avatar.jpg',  N'Logo Phở Thìn',              'avatar',  0, 3),
(7,  'https://cdn.example.com/r3-banner.jpg',  N'Tô phở đặc trưng',           'banner',  0, 3),
(8,  'https://cdn.example.com/r4-avatar.jpg',  N'Logo Nhà Hàng Ngon',         'avatar',  0, 4),
(9,  'https://cdn.example.com/r4-banner.jpg',  N'Biệt thự Pháp cổ điển',      'banner',  0, 4),
(10, 'https://cdn.example.com/r5-avatar.jpg',  N'Logo Cơm Tấm Thuận Kiều',    'avatar',  0, 5),
(11, 'https://cdn.example.com/r5-banner.jpg',  N'Dĩa cơm tấm sườn bì chả',   'banner',  0, 5);
SET IDENTITY_INSERT [RestaurantImage] OFF;

-- -----------------------------------------------------
-- Language
-- -----------------------------------------------------
SET IDENTITY_INSERT [Language] ON;
INSERT INTO [Language] (id, code, name) VALUES
(1, 'vi', N'Tiếng Việt'),
(2, 'en', 'English'),
(3, 'ja', N'日本語');
SET IDENTITY_INSERT [Language] OFF;

-- -----------------------------------------------------
-- RestaurantContent
-- -----------------------------------------------------
SET IDENTITY_INSERT [RestaurantContent] ON;
INSERT INTO [RestaurantContent] (id, title, description, ttsScript, restaurantId, languageId) VALUES
-- Chả Cá Lã Vọng
(1,  N'Chả Cá Lã Vọng',       N'Nhà hàng chả cá lâu đời nhất Hà Nội, hoạt động từ thế kỷ 19.',          N'Chào mừng bạn đến Chả Cá Lã Vọng, nhà hàng với hơn 100 năm lịch sử tại phố Chả Cá, Hà Nội.',                    1, 1),
(2,  'Cha Ca La Vong',         'One of Hanoi oldest and most iconic fish restaurants, operating since the 19th century.', 'Welcome to Cha Ca La Vong, a restaurant with over 100 years of history on Cha Ca Street, Hanoi.', 1, 2),
(3,  N'チャーカーラーヴォン',  N'19世紀から続くハノイ最古の魚料理レストラン。',                            N'ハノイのチャーカー通りにある、100年以上の歴史を持つチャーカーラーヴォンへようこそ。',                          1, 3),
-- Bún Chả Hương Liên
(4,  N'Bún Chả Hương Liên',   N'Quán bún chả nổi tiếng thế giới sau chuyến thăm của Tổng thống Obama.',  N'Bạn đang đứng trước Bún Chả Hương Liên, nơi Tổng thống Obama và đầu bếp Anthony Bourdain đã thưởng thức bún chả năm 2016.', 2, 1),
(5,  'Bun Cha Huong Lien',    'World-famous bun cha restaurant visited by President Obama in 2016.',      'You are standing in front of Bun Cha Huong Lien, where President Obama and chef Anthony Bourdain enjoyed bun cha in 2016.', 2, 2),
-- Phở Thìn Bờ Hồ
(6,  N'Phở Thìn Bờ Hồ',      N'Thương hiệu phở bò xào nổi tiếng nhất Hà Nội với công thức gia truyền.', N'Chào mừng đến Phở Thìn Bờ Hồ, nơi lưu giữ hương vị phở bò xào truyền thống Hà Nội từ hàng chục năm qua.',     3, 1),
(7,  'Pho Thin Bo Ho',        'Hanoi most famous stir-fried beef pho with a secret family recipe.',       'Welcome to Pho Thin Bo Ho, preserving the traditional Hanoi stir-fried beef pho flavor for decades.',              3, 2),
-- Nhà Hàng Ngon
(8,  N'Nhà Hàng Ngon',        N'Ẩm thực ba miền Việt Nam trong không gian biệt thự Pháp đầu thế kỷ 20.', N'Chào mừng đến Nhà Hàng Ngon, nơi hội tụ tinh hoa ẩm thực ba miền Bắc Trung Nam trong không gian kiến trúc Pháp cổ điển.', 4, 1),
(9,  'Ngon Restaurant',       'Vietnamese cuisine from all three regions in a classic early 20th century French villa.', 'Welcome to Ngon Restaurant, where the culinary essence of North, Central, and South Vietnam meets in a classic French colonial setting.', 4, 2),
-- Cơm Tấm Thuận Kiều
(10, N'Cơm Tấm Thuận Kiều',  N'Thương hiệu cơm tấm sườn bì chả đặc trưng Sài Gòn từ năm 1975.',        N'Bạn đang đứng trước Cơm Tấm Thuận Kiều, một trong những tiệm cơm tấm lâu đời và nổi tiếng nhất Sài Gòn.',      5, 1),
(11, 'Com Tam Thuan Kieu',    'Iconic Saigon broken rice restaurant serving since 1975.',                 'You are in front of Com Tam Thuan Kieu, one of the oldest and most beloved broken rice restaurants in Saigon.',    5, 2);
SET IDENTITY_INSERT [RestaurantContent] OFF;

-- -----------------------------------------------------
-- RestaurantAudio
-- -----------------------------------------------------
SET IDENTITY_INSERT [RestaurantAudio] ON;
INSERT INTO [RestaurantAudio] (id, audioUrl, duration, restaurantId, languageId) VALUES
(1,  'https://cdn.example.com/audio/r1-vi.mp3', 32.5, 1, 1),
(2,  'https://cdn.example.com/audio/r1-en.mp3', 28.0, 1, 2),
(3,  'https://cdn.example.com/audio/r1-ja.mp3', 35.0, 1, 3),
(4,  'https://cdn.example.com/audio/r2-vi.mp3', 40.0, 2, 1),
(5,  'https://cdn.example.com/audio/r2-en.mp3', 36.5, 2, 2),
(6,  'https://cdn.example.com/audio/r3-vi.mp3', 30.0, 3, 1),
(7,  'https://cdn.example.com/audio/r3-en.mp3', 27.5, 3, 2),
(8,  'https://cdn.example.com/audio/r4-vi.mp3', 38.0, 4, 1),
(9,  'https://cdn.example.com/audio/r4-en.mp3', 34.0, 4, 2),
(10, 'https://cdn.example.com/audio/r5-vi.mp3', 33.0, 5, 1),
(11, 'https://cdn.example.com/audio/r5-en.mp3', 29.5, 5, 2);
SET IDENTITY_INSERT [RestaurantAudio] OFF;

-- -----------------------------------------------------
-- VisitLog
-- -----------------------------------------------------
SET IDENTITY_INSERT [VisitLog] ON;
INSERT INTO [VisitLog] (id, visitTime, latitude, longitude, userId, restaurantId) VALUES
(1, '2024-03-01 09:10:00', 21.033860, 105.848145, 4, 1),
(2, '2024-03-01 10:30:00', 21.021770, 105.851188, 4, 2),
(3, '2024-03-02 11:00:00', 21.028612, 105.852391, 5, 3),
(4, '2024-03-05 14:20:00', 10.779645, 106.694815, 4, 4),
(5, '2024-03-06 08:45:00', 10.772935, 106.686505, 5, 5),
(6, '2024-03-07 09:05:00', 21.033855, 105.848140, 5, 1);
SET IDENTITY_INSERT [VisitLog] OFF;

-- -----------------------------------------------------
-- AudioPlayLog
-- -----------------------------------------------------
SET IDENTITY_INSERT [AudioPlayLog] ON;
INSERT INTO [AudioPlayLog] (id, playTime, durationListened, userId, restaurantId, languageId) VALUES
(1, '2024-03-01 09:10:30', 32.5, 4, 1, 1),
(2, '2024-03-01 10:30:45', 40.0, 4, 2, 1),
(3, '2024-03-02 11:00:20', 27.5, 5, 3, 2),
(4, '2024-03-05 14:21:00', 34.0, 4, 4, 2),
(5, '2024-03-06 08:45:30', 29.5, 5, 5, 2),
(6, '2024-03-07 09:05:15', 28.0, 5, 1, 2);
SET IDENTITY_INSERT [AudioPlayLog] OFF;
