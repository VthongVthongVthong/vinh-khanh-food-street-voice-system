BEGIN TRANSACTION;
CREATE TABLE AudioPlayLog (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    playTime         TEXT NOT NULL DEFAULT (datetime('now')),
    durationListened REAL,
    userId           INTEGER NOT NULL,
    poiId            INTEGER NOT NULL,
    languageId       INTEGER NOT NULL,
    FOREIGN KEY (userId)     REFERENCES User(id),
    FOREIGN KEY (poiId)      REFERENCES POI(id),
    FOREIGN KEY (languageId) REFERENCES Language(id)
);
INSERT INTO "AudioPlayLog" VALUES(1,'2024-03-01 19:10:30',28.5,3,1,1);
INSERT INTO "AudioPlayLog" VALUES(2,'2024-03-01 20:05:20',31.0,3,2,1);
INSERT INTO "AudioPlayLog" VALUES(3,'2024-03-02 19:30:15',25.0,4,3,2);
INSERT INTO "AudioPlayLog" VALUES(4,'2024-03-03 20:00:10',33.0,4,4,1);
INSERT INTO "AudioPlayLog" VALUES(5,'2024-03-04 21:00:05',29.5,5,9,2);
INSERT INTO "AudioPlayLog" VALUES(6,'2024-03-05 20:30:20',27.0,3,7,1);
CREATE TABLE Language (
    id   INTEGER PRIMARY KEY AUTOINCREMENT,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL
);
INSERT INTO "Language" VALUES(1,'vi','Tiếng Việt');
INSERT INTO "Language" VALUES(2,'en','English');
INSERT INTO "Language" VALUES(3,'ja','日本語');
CREATE TABLE POI (
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    name                TEXT NOT NULL,
    latitude            REAL NOT NULL,
    longitude           REAL NOT NULL,
    address             TEXT,
    phone               TEXT,
    descriptionText     TEXT,
    imageUrls           TEXT,
    language            TEXT NOT NULL DEFAULT 'vi',
    mapLink             TEXT,
    triggerRadiusMeters INTEGER NOT NULL DEFAULT 20,
    isActive            INTEGER NOT NULL DEFAULT 1,
    createdAt           TEXT NOT NULL DEFAULT (datetime('now')),
    updatedAt           TEXT NOT NULL DEFAULT (datetime('now')),
    ownerId             INTEGER NOT NULL,
    FOREIGN KEY (ownerId) REFERENCES User(id)
);
INSERT INTO "POI" VALUES(1,'Ốc Oanh',10.7612,106.7018,'534 Vĩnh Khánh, P.8, Q.4','0909123001','Quán ốc lâu đời nổi tiếng nhất phố Vĩnh Khánh.','["https://cdn.vinhkhanh.vn/img/poi1-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi1-banner.jpg"]','vi','https://maps.app.goo.gl/oc-oanh',25,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(2,'Ốc Thảo',10.7605,106.7015,'383 Vĩnh Khánh, P.8, Q.4','0388004422','Quán ốc rộng rãi, nổi tiếng món nướng và sốt trứng muối.','["https://cdn.vinhkhanh.vn/img/poi2-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi2-banner.jpg"]','vi','https://maps.app.goo.gl/oc-thao',25,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(3,'Ốc Đào 2',10.759,106.7008,'123 Vĩnh Khánh, P.8, Q.4','0352499883','Hơn 30 loại ốc, nước sốt trứng muối và xào me đặc trưng.','["https://cdn.vinhkhanh.vn/img/poi3-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi3-banner.jpg"]','vi','https://maps.app.goo.gl/oc-dao2',20,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(4,'Ốc Vũ',10.7582,106.7003,'37 Vĩnh Khánh, P.8, Q.4','0908935592','Biểu tượng ẩm thực Quận 4, mở xuyên đêm.','["https://cdn.vinhkhanh.vn/img/poi4-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi4-banner.jpg"]','vi','https://maps.app.goo.gl/oc-vu',25,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(5,'Ốc Sáu Nở',10.75895,106.70075,'121 Vĩnh Khánh, P.8, Q.4','0909123005','Hải sản tươi sống, nước chấm tự pha đặc biệt.','["https://cdn.vinhkhanh.vn/img/poi5-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi5-banner.jpg"]','vi','https://maps.app.goo.gl/oc-sau-no',20,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(6,'Lãng Quán',10.7597,106.7011,'232 Vĩnh Khánh, P.10, Q.4','0909123006','Ăn vặt đặc sắc: giò heo chiên, dồi vịt, cà kèo nướng.','["https://cdn.vinhkhanh.vn/img/poi6-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi6-banner.jpg"]','vi','https://maps.app.goo.gl/lang-quan',20,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(7,'Ớt Xiêm Quán',10.7615,106.702,'568 Vĩnh Khánh, P.8, Q.4','0983434926','Hải sản & nướng, cá diêu hồng rang muối và tôm mù tạt.','["https://cdn.vinhkhanh.vn/img/poi7-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi7-banner.jpg"]','vi','https://maps.app.goo.gl/ot-xiem',20,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(8,'Thế Giới Bò',10.758,106.7002,'6 Vĩnh Khánh, P.9, Q.4','0907988770','Chuyên các món bò: phở, bò chảo, lẩu bò.','["https://cdn.vinhkhanh.vn/img/poi8-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi8-banner.jpg"]','vi','https://maps.app.goo.gl/the-gioi-bo',20,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(9,'Chilli Lẩu Nướng',10.75965,106.70108,'232/123 Vĩnh Khánh, Q.4','0902935667','Lẩu & nướng tự chọn, gà cay phô mai Hàn Quốc đặc trưng.','["https://cdn.vinhkhanh.vn/img/poi9-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi9-banner.jpg"]','vi','https://maps.app.goo.gl/chilli',20,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(10,'Tỷ Muội Quán',10.75825,106.70032,'39 Vĩnh Khánh, P.8, Q.4','0909123010','Quán bình dân cho nhóm bạn, menu đa dạng, giá rẻ.','["https://cdn.vinhkhanh.vn/img/poi10-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi10-banner.jpg"]','vi','https://maps.app.goo.gl/ty-muoi',20,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(11,'Alo Quán Seafood',10.7603,106.70135,'333 Vĩnh Khánh, Q.4','0909123011','Seafood & Beer, nhậu ngon bổ rẻ.','["https://cdn.vinhkhanh.vn/img/poi11-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi11-banner.jpg"]','vi','https://maps.app.goo.gl/alo-quan',25,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(12,'Quán 3 Cô Tiên',10.75905,106.70078,'128 Vĩnh Khánh, P.8, Q.4','0909123012','Mở cửa gần sáng, tụ điểm hàn huyên về đêm.','["https://cdn.vinhkhanh.vn/img/poi12-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi12-banner.jpg"]','vi','https://maps.app.goo.gl/3-co-tien',20,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(13,'A Fat Hot Pot',10.7593,106.7009,'180 Vĩnh Khánh, P.8, Q.4','0909123013','Lẩu hàu kim chi và mực nướng sa tế, không gian trẻ trung.','["https://cdn.vinhkhanh.vn/img/poi13-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi13-banner.jpg"]','vi','https://maps.app.goo.gl/a-fat',20,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(14,'Sườn Nướng Ớt',10.7598,106.70115,'250 Vĩnh Khánh, P.8, Q.4','0909123014','Sườn nướng than hoa, mùi thơm đặc trưng cả con phố.','["https://cdn.vinhkhanh.vn/img/poi14-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi14-banner.jpg"]','vi','https://maps.app.goo.gl/suon-nuong',20,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(15,'Ốc Len Xào Dừa',10.76135,106.70193,'531 Vĩnh Khánh, P.10, Q.4','0888833111','Chuyên ốc len xào dừa và ốc bươu nhồi thịt.','["https://cdn.vinhkhanh.vn/img/poi15-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi15-banner.jpg"]','vi','https://maps.app.goo.gl/oc-len',20,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(16,'Quán Hỏa',10.7601,106.70125,'300 Vĩnh Khánh, P.8, Q.4','0909123016','Lẩu và nướng sôi động, nhộn nhịp nhất về đêm.','["https://cdn.vinhkhanh.vn/img/poi16-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi16-banner.jpg"]','vi','https://maps.app.goo.gl/quan-hoa',20,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(17,'Hải Sản Biển Xanh',10.76085,106.70163,'450 Vĩnh Khánh, P.8, Q.4','0909123017','Hải sản tươi sống theo kg: cua, ghẹ, tôm hùm.','["https://cdn.vinhkhanh.vn/img/poi17-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi17-banner.jpg"]','vi','https://maps.app.goo.gl/bien-xanh',25,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(18,'Nướng Vỉa Hè VK',10.75945,106.70098,'200 Vĩnh Khánh, P.8, Q.4','0909123018','Nướng vỉa hè Sài Gòn: bạch tuộc, mực, bắp nướng.','["https://cdn.vinhkhanh.vn/img/poi18-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi18-banner.jpg"]','vi','https://maps.app.goo.gl/nuong-vh',20,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(19,'Lẩu Bò Vĩnh Khánh',10.7586,106.70052,'90 Vĩnh Khánh, P.8, Q.4','0909123019','Lẩu bò tươi ngon, nước lẩu đậm đà cho nhóm đông.','["https://cdn.vinhkhanh.vn/img/poi19-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi19-banner.jpg"]','vi','https://maps.app.goo.gl/lau-bo',20,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
INSERT INTO "POI" VALUES(20,'Mực Nướng Sa Tế',10.7607,106.70157,'420 Vĩnh Khánh, P.8, Q.4','0909123020','Chuyên mực và hải sản nướng than hoa.','["https://cdn.vinhkhanh.vn/img/poi20-avatar.jpg", "https://cdn.vinhkhanh.vn/img/poi20-banner.jpg"]','vi','https://maps.app.goo.gl/muc-nuong',20,1,'2026-03-16 12:14:30','2026-03-16 12:14:30',2);
CREATE TABLE POIAudio (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    audioUrl   TEXT,
    duration   REAL,
    poiId      INTEGER NOT NULL,
    languageId INTEGER NOT NULL,
    FOREIGN KEY (poiId)      REFERENCES POI(id),
    FOREIGN KEY (languageId) REFERENCES Language(id),
    UNIQUE (poiId, languageId)
);
INSERT INTO "POIAudio" VALUES(1,'https://cdn.vinhkhanh.vn/audio/poi1-vi.mp3',NULL,1,1);
INSERT INTO "POIAudio" VALUES(2,'https://cdn.vinhkhanh.vn/audio/poi1-en.mp3',NULL,1,2);
INSERT INTO "POIAudio" VALUES(3,'https://cdn.vinhkhanh.vn/audio/poi2-vi.mp3',NULL,2,1);
INSERT INTO "POIAudio" VALUES(4,'https://cdn.vinhkhanh.vn/audio/poi2-en.mp3',NULL,2,2);
INSERT INTO "POIAudio" VALUES(5,'https://cdn.vinhkhanh.vn/audio/poi3-vi.mp3',NULL,3,1);
INSERT INTO "POIAudio" VALUES(6,'https://cdn.vinhkhanh.vn/audio/poi3-en.mp3',NULL,3,2);
INSERT INTO "POIAudio" VALUES(7,'https://cdn.vinhkhanh.vn/audio/poi4-vi.mp3',NULL,4,1);
INSERT INTO "POIAudio" VALUES(8,'https://cdn.vinhkhanh.vn/audio/poi4-en.mp3',NULL,4,2);
INSERT INTO "POIAudio" VALUES(9,'https://cdn.vinhkhanh.vn/audio/poi5-vi.mp3',NULL,5,1);
INSERT INTO "POIAudio" VALUES(10,'https://cdn.vinhkhanh.vn/audio/poi5-en.mp3',NULL,5,2);
INSERT INTO "POIAudio" VALUES(11,'https://cdn.vinhkhanh.vn/audio/poi6-vi.mp3',NULL,6,1);
INSERT INTO "POIAudio" VALUES(12,'https://cdn.vinhkhanh.vn/audio/poi6-en.mp3',NULL,6,2);
INSERT INTO "POIAudio" VALUES(13,'https://cdn.vinhkhanh.vn/audio/poi7-vi.mp3',NULL,7,1);
INSERT INTO "POIAudio" VALUES(14,'https://cdn.vinhkhanh.vn/audio/poi7-en.mp3',NULL,7,2);
INSERT INTO "POIAudio" VALUES(15,'https://cdn.vinhkhanh.vn/audio/poi8-vi.mp3',NULL,8,1);
INSERT INTO "POIAudio" VALUES(16,'https://cdn.vinhkhanh.vn/audio/poi8-en.mp3',NULL,8,2);
INSERT INTO "POIAudio" VALUES(17,'https://cdn.vinhkhanh.vn/audio/poi9-vi.mp3',NULL,9,1);
INSERT INTO "POIAudio" VALUES(18,'https://cdn.vinhkhanh.vn/audio/poi9-en.mp3',NULL,9,2);
INSERT INTO "POIAudio" VALUES(19,'https://cdn.vinhkhanh.vn/audio/poi10-vi.mp3',NULL,10,1);
INSERT INTO "POIAudio" VALUES(20,'https://cdn.vinhkhanh.vn/audio/poi10-en.mp3',NULL,10,2);
INSERT INTO "POIAudio" VALUES(21,'https://cdn.vinhkhanh.vn/audio/poi11-vi.mp3',NULL,11,1);
INSERT INTO "POIAudio" VALUES(22,'https://cdn.vinhkhanh.vn/audio/poi11-en.mp3',NULL,11,2);
INSERT INTO "POIAudio" VALUES(23,'https://cdn.vinhkhanh.vn/audio/poi12-vi.mp3',NULL,12,1);
INSERT INTO "POIAudio" VALUES(24,'https://cdn.vinhkhanh.vn/audio/poi12-en.mp3',NULL,12,2);
INSERT INTO "POIAudio" VALUES(25,'https://cdn.vinhkhanh.vn/audio/poi13-vi.mp3',NULL,13,1);
INSERT INTO "POIAudio" VALUES(26,'https://cdn.vinhkhanh.vn/audio/poi13-en.mp3',NULL,13,2);
INSERT INTO "POIAudio" VALUES(27,'https://cdn.vinhkhanh.vn/audio/poi14-vi.mp3',NULL,14,1);
INSERT INTO "POIAudio" VALUES(28,'https://cdn.vinhkhanh.vn/audio/poi14-en.mp3',NULL,14,2);
INSERT INTO "POIAudio" VALUES(29,'https://cdn.vinhkhanh.vn/audio/poi15-vi.mp3',NULL,15,1);
INSERT INTO "POIAudio" VALUES(30,'https://cdn.vinhkhanh.vn/audio/poi15-en.mp3',NULL,15,2);
INSERT INTO "POIAudio" VALUES(31,'https://cdn.vinhkhanh.vn/audio/poi16-vi.mp3',NULL,16,1);
INSERT INTO "POIAudio" VALUES(32,'https://cdn.vinhkhanh.vn/audio/poi16-en.mp3',NULL,16,2);
INSERT INTO "POIAudio" VALUES(33,'https://cdn.vinhkhanh.vn/audio/poi17-vi.mp3',NULL,17,1);
INSERT INTO "POIAudio" VALUES(34,'https://cdn.vinhkhanh.vn/audio/poi17-en.mp3',NULL,17,2);
INSERT INTO "POIAudio" VALUES(35,'https://cdn.vinhkhanh.vn/audio/poi18-vi.mp3',NULL,18,1);
INSERT INTO "POIAudio" VALUES(36,'https://cdn.vinhkhanh.vn/audio/poi18-en.mp3',NULL,18,2);
INSERT INTO "POIAudio" VALUES(37,'https://cdn.vinhkhanh.vn/audio/poi19-vi.mp3',NULL,19,1);
INSERT INTO "POIAudio" VALUES(38,'https://cdn.vinhkhanh.vn/audio/poi19-en.mp3',NULL,19,2);
INSERT INTO "POIAudio" VALUES(39,'https://cdn.vinhkhanh.vn/audio/poi20-vi.mp3',NULL,20,1);
INSERT INTO "POIAudio" VALUES(40,'https://cdn.vinhkhanh.vn/audio/poi20-en.mp3',NULL,20,2);
CREATE TABLE POIContent (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    title        TEXT,
    description  TEXT,
    ttsScript    TEXT,
    poiId        INTEGER NOT NULL,
    languageId   INTEGER NOT NULL,
    FOREIGN KEY (poiId)      REFERENCES POI(id),
    FOREIGN KEY (languageId) REFERENCES Language(id),
    UNIQUE (poiId, languageId)
);
INSERT INTO "POIContent" VALUES(1,'Ốc Oanh',NULL,'Chào mừng đến Ốc Oanh, quán ốc lâu đời và nổi tiếng nhất trên phố ẩm thực Vĩnh Khánh Quận 4.',1,1);
INSERT INTO "POIContent" VALUES(2,'Ốc Oanh',NULL,'Welcome to Oc Oanh, the most famous seafood snail restaurant on Vinh Khanh food street.',1,2);
INSERT INTO "POIContent" VALUES(3,'Ốc Thảo',NULL,'Bạn đang đứng trước Ốc Thảo, quán ốc rộng rãi với thực đơn đa dạng và các món nướng thơm ngon.',2,1);
INSERT INTO "POIContent" VALUES(4,'Ốc Thảo',NULL,'You are at Oc Thao, a spacious seafood restaurant known for fresh shellfish and grilled dishes.',2,2);
INSERT INTO "POIContent" VALUES(5,'Ốc Đào 2',NULL,'Ốc Đào 2 có hơn 30 loại ốc, đặc biệt nổi tiếng với sốt trứng muối và xào me đậm đà.',3,1);
INSERT INTO "POIContent" VALUES(6,'Ốc Đào 2',NULL,'Oc Dao 2 offers over 30 types of snails, famous for its salted egg sauce and tamarind stir-fry.',3,2);
INSERT INTO "POIContent" VALUES(7,'Ốc Vũ',NULL,'Ốc Vũ là biểu tượng ẩm thực Quận 4 với hơn 10 năm kinh nghiệm, mở cửa xuyên đêm phục vụ thực khách.',4,1);
INSERT INTO "POIContent" VALUES(8,'Ốc Vũ',NULL,'Oc Vu is a District 4 culinary icon with over 10 years of service, open through the night.',4,2);
INSERT INTO "POIContent" VALUES(9,'Ốc Sáu Nở',NULL,'Chào mừng đến Ốc Sáu Nở, hải sản tươi ngon mỗi ngày và nước chấm tự pha đặc biệt.',5,1);
INSERT INTO "POIContent" VALUES(10,'Ốc Sáu Nở',NULL,'Welcome to Oc Sau No, where fresh seafood meets the unique homemade dipping sauce.',5,2);
INSERT INTO "POIContent" VALUES(11,'Lãng Quán',NULL,'Lãng Quán nổi tiếng với giò heo muối chiên giòn, dồi vịt và cà kèo nướng muối ớt đặc sắc.',6,1);
INSERT INTO "POIContent" VALUES(12,'Lãng Quán',NULL,'Lang Quan is famous for crispy salted pork leg, duck sausage and grilled mudskipper.',6,2);
INSERT INTO "POIContent" VALUES(13,'Ớt Xiêm Quán',NULL,'Ớt Xiêm Quán chuyên hải sản tươi và nướng, nổi bật nhất là cá diêu hồng rang muối và tôm sú mù tạt.',7,1);
INSERT INTO "POIContent" VALUES(14,'Ớt Xiêm Quán',NULL,'Ot Xiem Quan specializes in fresh seafood, famous for salted red tilapia and mustard shrimp.',7,2);
INSERT INTO "POIContent" VALUES(15,'Thế Giới Bò',NULL,'Thế Giới Bò chuyên các món bò tươi ngon từ phở và bò chảo đến lẩu bò đậm đà.',8,1);
INSERT INTO "POIContent" VALUES(16,'Thế Giới Bò',NULL,'The Beef World specializes in premium beef dishes including pho, sizzling beef and beef hotpot.',8,2);
INSERT INTO "POIContent" VALUES(17,'Chilli Lẩu Nướng',NULL,'Chilli phục vụ lẩu và nướng tự chọn, nổi bật với gà cay phô mai Hàn Quốc và tôm sốt trứng muối.',9,1);
INSERT INTO "POIContent" VALUES(18,'Chilli Lẩu Nướng',NULL,'Chilli offers all-you-can-grill with signature Korean cheesy spicy chicken and salted egg shrimp.',9,2);
INSERT INTO "POIContent" VALUES(19,'Tỷ Muội Quán',NULL,'Tỷ Muội Quán là quán bình dân với menu đa dạng, phù hợp cho nhóm bạn tụ tập ăn uống.',10,1);
INSERT INTO "POIContent" VALUES(20,'Tỷ Muội Quán',NULL,'Ty Muoi Quan is a casual budget-friendly spot with diverse menu, perfect for group gatherings.',10,2);
INSERT INTO "POIContent" VALUES(21,'Alo Quán Seafood',NULL,'Alo Quán Seafood & Beer là nơi thưởng thức hải sản tươi cùng bia lạnh với giá bình dân.',11,1);
INSERT INTO "POIContent" VALUES(22,'Alo Quán Seafood',NULL,'Alo Quan Seafood and Beer is ideal for fresh seafood paired with cold beer at friendly prices.',11,2);
INSERT INTO "POIContent" VALUES(23,'Quán 3 Cô Tiên',NULL,'Quán 3 Cô Tiên mở đến gần sáng, tụ điểm cho những nhóm bạn thích hàn huyên đêm khuya.',12,1);
INSERT INTO "POIContent" VALUES(24,'Quán 3 Cô Tiên',NULL,'Quan 3 Co Tien stays open until near dawn, a popular late-night gathering spot.',12,2);
INSERT INTO "POIContent" VALUES(25,'A Fat Hot Pot',NULL,'A Fat Hot Pot nổi tiếng với lẩu hàu kim chi và mực nướng sa tế, không gian trẻ trung sôi động.',13,1);
INSERT INTO "POIContent" VALUES(26,'A Fat Hot Pot',NULL,'A Fat Hot Pot is known for oyster kimchi hotpot and sambal grilled squid in a vibrant atmosphere.',13,2);
INSERT INTO "POIContent" VALUES(27,'Sườn Nướng Ớt',NULL,'Sườn Nướng Ớt chuyên sườn nướng than hoa, mùi khói thơm lừng lan tỏa khắp phố Vĩnh Khánh.',14,1);
INSERT INTO "POIContent" VALUES(28,'Sườn Nướng Ớt',NULL,'Suon Nuong Ot specializes in charcoal-grilled ribs, its smoky aroma filling Vinh Khanh street.',14,2);
INSERT INTO "POIContent" VALUES(29,'Ốc Len Xào Dừa',NULL,'Ốc Len Xào Dừa nổi tiếng với ốc len xào dừa đặc trưng và ốc bươu nhồi thịt hấp dẫn.',15,1);
INSERT INTO "POIContent" VALUES(30,'Ốc Len Xào Dừa',NULL,'Oc Len Xao Dua is famous for signature coconut stir-fried mud creepers and stuffed snails.',15,2);
INSERT INTO "POIContent" VALUES(31,'Quán Hỏa',NULL,'Quán Hỏa là điểm đến sôi động với lẩu hàu kim chi và món nướng hấp dẫn về buổi tối.',16,1);
INSERT INTO "POIContent" VALUES(32,'Quán Hỏa',NULL,'Quan Hoa is a lively spot with oyster kimchi hotpot and exciting grilled dishes in the evenings.',16,2);
INSERT INTO "POIContent" VALUES(33,'Hải Sản Biển Xanh',NULL,'Hải Sản Biển Xanh cung cấp cua, ghẹ và tôm hùm tươi sống theo kg với giá thị trường.',17,1);
INSERT INTO "POIContent" VALUES(34,'Hải Sản Biển Xanh',NULL,'Bien Xanh Seafood offers live crabs and lobsters by kilogram at transparent market prices.',17,2);
INSERT INTO "POIContent" VALUES(35,'Nướng Vỉa Hè VK',NULL,'Nướng Vỉa Hè Vĩnh Khánh mang chất đường phố Sài Gòn với bạch tuộc, mực và bắp nướng.',18,1);
INSERT INTO "POIContent" VALUES(36,'Nướng Vỉa Hè VK',NULL,'VK Street Grill embodies Saigon street food with fragrant grilled octopus, squid and corn.',18,2);
INSERT INTO "POIContent" VALUES(37,'Lẩu Bò Vĩnh Khánh',NULL,'Lẩu Bò Vĩnh Khánh phục vụ lẩu bò nước đậm đà, thích hợp cho nhóm đông người tụ tập.',19,1);
INSERT INTO "POIContent" VALUES(38,'Lẩu Bò Vĩnh Khánh',NULL,'Vinh Khanh Beef Hotpot serves rich broth beef hotpot, perfect for large group gatherings.',19,2);
INSERT INTO "POIContent" VALUES(39,'Mực Nướng Sa Tế',NULL,'Mực Nướng Sa Tế chuyên hải sản nướng than hoa, nổi tiếng nhất với mực và tôm nướng sa tế.',20,1);
INSERT INTO "POIContent" VALUES(40,'Mực Nướng Sa Tế',NULL,'Sambal Grilled Squid specializes in charcoal-grilled seafood, famous for sambal squid and shrimp.',20,2);
CREATE TABLE POIImage (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    imageUrl  TEXT NOT NULL,
    caption   TEXT,
    imageType TEXT CHECK (imageType IN ('avatar','banner','gallery')),
    sortOrder INTEGER NOT NULL DEFAULT 0,
    poiId     INTEGER NOT NULL,
    FOREIGN KEY (poiId) REFERENCES POI(id)
);
INSERT INTO "POIImage" VALUES(1,'https://cdn.vinhkhanh.vn/img/poi1-avatar.jpg',NULL,'avatar',0,1);
INSERT INTO "POIImage" VALUES(2,'https://cdn.vinhkhanh.vn/img/poi1-banner.jpg',NULL,'banner',0,1);
INSERT INTO "POIImage" VALUES(3,'https://cdn.vinhkhanh.vn/img/poi2-avatar.jpg',NULL,'avatar',0,2);
INSERT INTO "POIImage" VALUES(4,'https://cdn.vinhkhanh.vn/img/poi2-banner.jpg',NULL,'banner',0,2);
INSERT INTO "POIImage" VALUES(5,'https://cdn.vinhkhanh.vn/img/poi3-avatar.jpg',NULL,'avatar',0,3);
INSERT INTO "POIImage" VALUES(6,'https://cdn.vinhkhanh.vn/img/poi3-banner.jpg',NULL,'banner',0,3);
INSERT INTO "POIImage" VALUES(7,'https://cdn.vinhkhanh.vn/img/poi4-avatar.jpg',NULL,'avatar',0,4);
INSERT INTO "POIImage" VALUES(8,'https://cdn.vinhkhanh.vn/img/poi4-banner.jpg',NULL,'banner',0,4);
INSERT INTO "POIImage" VALUES(9,'https://cdn.vinhkhanh.vn/img/poi5-avatar.jpg',NULL,'avatar',0,5);
INSERT INTO "POIImage" VALUES(10,'https://cdn.vinhkhanh.vn/img/poi5-banner.jpg',NULL,'banner',0,5);
INSERT INTO "POIImage" VALUES(11,'https://cdn.vinhkhanh.vn/img/poi6-avatar.jpg',NULL,'avatar',0,6);
INSERT INTO "POIImage" VALUES(12,'https://cdn.vinhkhanh.vn/img/poi6-banner.jpg',NULL,'banner',0,6);
INSERT INTO "POIImage" VALUES(13,'https://cdn.vinhkhanh.vn/img/poi7-avatar.jpg',NULL,'avatar',0,7);
INSERT INTO "POIImage" VALUES(14,'https://cdn.vinhkhanh.vn/img/poi7-banner.jpg',NULL,'banner',0,7);
INSERT INTO "POIImage" VALUES(15,'https://cdn.vinhkhanh.vn/img/poi8-avatar.jpg',NULL,'avatar',0,8);
INSERT INTO "POIImage" VALUES(16,'https://cdn.vinhkhanh.vn/img/poi8-banner.jpg',NULL,'banner',0,8);
INSERT INTO "POIImage" VALUES(17,'https://cdn.vinhkhanh.vn/img/poi9-avatar.jpg',NULL,'avatar',0,9);
INSERT INTO "POIImage" VALUES(18,'https://cdn.vinhkhanh.vn/img/poi9-banner.jpg',NULL,'banner',0,9);
INSERT INTO "POIImage" VALUES(19,'https://cdn.vinhkhanh.vn/img/poi10-avatar.jpg',NULL,'avatar',0,10);
INSERT INTO "POIImage" VALUES(20,'https://cdn.vinhkhanh.vn/img/poi10-banner.jpg',NULL,'banner',0,10);
INSERT INTO "POIImage" VALUES(21,'https://cdn.vinhkhanh.vn/img/poi11-avatar.jpg',NULL,'avatar',0,11);
INSERT INTO "POIImage" VALUES(22,'https://cdn.vinhkhanh.vn/img/poi11-banner.jpg',NULL,'banner',0,11);
INSERT INTO "POIImage" VALUES(23,'https://cdn.vinhkhanh.vn/img/poi12-avatar.jpg',NULL,'avatar',0,12);
INSERT INTO "POIImage" VALUES(24,'https://cdn.vinhkhanh.vn/img/poi12-banner.jpg',NULL,'banner',0,12);
INSERT INTO "POIImage" VALUES(25,'https://cdn.vinhkhanh.vn/img/poi13-avatar.jpg',NULL,'avatar',0,13);
INSERT INTO "POIImage" VALUES(26,'https://cdn.vinhkhanh.vn/img/poi13-banner.jpg',NULL,'banner',0,13);
INSERT INTO "POIImage" VALUES(27,'https://cdn.vinhkhanh.vn/img/poi14-avatar.jpg',NULL,'avatar',0,14);
INSERT INTO "POIImage" VALUES(28,'https://cdn.vinhkhanh.vn/img/poi14-banner.jpg',NULL,'banner',0,14);
INSERT INTO "POIImage" VALUES(29,'https://cdn.vinhkhanh.vn/img/poi15-avatar.jpg',NULL,'avatar',0,15);
INSERT INTO "POIImage" VALUES(30,'https://cdn.vinhkhanh.vn/img/poi15-banner.jpg',NULL,'banner',0,15);
INSERT INTO "POIImage" VALUES(31,'https://cdn.vinhkhanh.vn/img/poi16-avatar.jpg',NULL,'avatar',0,16);
INSERT INTO "POIImage" VALUES(32,'https://cdn.vinhkhanh.vn/img/poi16-banner.jpg',NULL,'banner',0,16);
INSERT INTO "POIImage" VALUES(33,'https://cdn.vinhkhanh.vn/img/poi17-avatar.jpg',NULL,'avatar',0,17);
INSERT INTO "POIImage" VALUES(34,'https://cdn.vinhkhanh.vn/img/poi17-banner.jpg',NULL,'banner',0,17);
INSERT INTO "POIImage" VALUES(35,'https://cdn.vinhkhanh.vn/img/poi18-avatar.jpg',NULL,'avatar',0,18);
INSERT INTO "POIImage" VALUES(36,'https://cdn.vinhkhanh.vn/img/poi18-banner.jpg',NULL,'banner',0,18);
INSERT INTO "POIImage" VALUES(37,'https://cdn.vinhkhanh.vn/img/poi19-avatar.jpg',NULL,'avatar',0,19);
INSERT INTO "POIImage" VALUES(38,'https://cdn.vinhkhanh.vn/img/poi19-banner.jpg',NULL,'banner',0,19);
INSERT INTO "POIImage" VALUES(39,'https://cdn.vinhkhanh.vn/img/poi20-avatar.jpg',NULL,'avatar',0,20);
INSERT INTO "POIImage" VALUES(40,'https://cdn.vinhkhanh.vn/img/poi20-banner.jpg',NULL,'banner',0,20);
CREATE TABLE User (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    username     TEXT NOT NULL UNIQUE,
    passwordHash TEXT NOT NULL,
    email        TEXT UNIQUE,
    phone        TEXT,
    role         TEXT NOT NULL CHECK (role IN ('ADMIN','OWNER','CUSTOMER')),
    createdAt    TEXT NOT NULL DEFAULT (datetime('now')),
    updatedAt    TEXT NOT NULL DEFAULT (datetime('now'))
);
INSERT INTO "User" VALUES(1,'admin','$2b$10$hash','admin@vinhkhanh.vn','0900000001','ADMIN','2024-01-01 00:00:00','2024-01-01 00:00:00');
INSERT INTO "User" VALUES(2,'owner_vinh','$2b$10$hash','owner@vinhkhanh.vn','0900000002','OWNER','2024-01-02 00:00:00','2024-01-02 00:00:00');
INSERT INTO "User" VALUES(3,'customer_01','$2b$10$hash','user1@gmail.com','0911000001','CUSTOMER','2024-02-01 00:00:00','2024-02-01 00:00:00');
INSERT INTO "User" VALUES(4,'customer_02','$2b$10$hash','user2@gmail.com','0911000002','CUSTOMER','2024-02-05 00:00:00','2024-02-05 00:00:00');
INSERT INTO "User" VALUES(5,'customer_03','$2b$10$hash','user3@gmail.com','0911000003','CUSTOMER','2024-02-10 00:00:00','2024-02-10 00:00:00');
CREATE TABLE VisitLog (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    visitTime TEXT NOT NULL DEFAULT (datetime('now')),
    latitude  REAL NOT NULL,
    longitude REAL NOT NULL,
    userId    INTEGER NOT NULL,
    poiId     INTEGER NOT NULL,
    FOREIGN KEY (userId) REFERENCES User(id),
    FOREIGN KEY (poiId)  REFERENCES POI(id)
);
INSERT INTO "VisitLog" VALUES(1,'2024-03-01 19:10:00',10.76118,106.70179,3,1);
INSERT INTO "VisitLog" VALUES(2,'2024-03-01 20:05:00',10.76048,106.70148,3,2);
INSERT INTO "VisitLog" VALUES(3,'2024-03-02 19:30:00',10.75898,106.70077,4,3);
INSERT INTO "VisitLog" VALUES(4,'2024-03-03 20:00:00',10.75818,106.70028,4,4);
INSERT INTO "VisitLog" VALUES(5,'2024-03-04 21:00:00',10.75962,106.70106,5,9);
INSERT INTO "VisitLog" VALUES(6,'2024-03-05 20:30:00',10.76148,106.70198,3,7);
CREATE INDEX IX_POI_LatLng ON POI (latitude, longitude);
CREATE INDEX IX_VisitLog_userId    ON VisitLog(userId);
CREATE INDEX IX_VisitLog_poiId     ON VisitLog(poiId);
CREATE INDEX IX_VisitLog_visitTime ON VisitLog(visitTime);
CREATE INDEX IX_Audio_poiId    ON AudioPlayLog(poiId);
CREATE INDEX IX_Audio_userId   ON AudioPlayLog(userId);
CREATE INDEX IX_Audio_playTime ON AudioPlayLog(playTime);
DELETE FROM "sqlite_sequence";
INSERT INTO "sqlite_sequence" VALUES('User',5);
INSERT INTO "sqlite_sequence" VALUES('POI',20);
INSERT INTO "sqlite_sequence" VALUES('Language',3);
INSERT INTO "sqlite_sequence" VALUES('POIContent',40);
INSERT INTO "sqlite_sequence" VALUES('POIAudio',40);
INSERT INTO "sqlite_sequence" VALUES('POIImage',40);
INSERT INTO "sqlite_sequence" VALUES('VisitLog',6);
INSERT INTO "sqlite_sequence" VALUES('AudioPlayLog',6);
COMMIT;
