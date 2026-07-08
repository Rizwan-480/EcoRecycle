-- EcoRecycle Seed Data Script
-- Target: SQL Server / LocalDB

USE EcoRecycleDb;
GO

-- Seed Roles
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Admin')
    INSERT INTO Roles (RoleName) VALUES ('Admin');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'RecyclingStore')
    INSERT INTO Roles (RoleName) VALUES ('RecyclingStore');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'User')
    INSERT INTO Roles (RoleName) VALUES ('User');
GO

-- Seed Waste Categories
DELETE FROM WasteCategories;
GO
INSERT INTO WasteCategories (Name, Description, PointsPerKg, IconUrl, IsRecyclable) VALUES
('Plastic', 'Bottles, containers, packaging, wrappers, cups', 30, 'bi bi-recycle', 1),
('Paper', 'Newspapers, cardboard, office paper, envelopes, boxes', 15, 'bi bi-file-earmark-text', 1),
('Glass', 'Jars, beverage bottles, glass shards, cosmetic jars', 20, 'bi bi-cup-straw', 1),
('Metal', 'Aluminum cans, steel food tins, copper pipes, foil', 40, 'bi bi-nut', 1),
('Electronics', 'Phones, computers, cables, batteries, chargers, small appliances', 50, 'bi bi-cpu', 1),
('Organic', 'Food waste, vegetable peels, leaves, coffee grounds', 10, 'bi bi-flower1', 1),
('Unknown', 'Items that cannot be identified automatically, to be checked manually', 0, 'bi bi-question-circle', 0);
GO

-- Seed Badges
DELETE FROM Badges;
GO
INSERT INTO Badges (Name, Description, IconUrl, ThresholdPoints) VALUES
('Green Starter', 'Awarded upon creating an account and joining the green revolution.', 'bi-door-open', 0),
('Eco Apprentice', 'Earned after collecting your first 100 points.', 'bi-award', 100),
('Zero-Waste Hero', 'Earned after collecting 500 points.', 'bi-shield-check', 500),
('Earth Guardian', 'Awarded to elite environmentalists who reach 1,000+ points.', 'bi-globe-americas', 1000);
GO

-- Seed Settings
DELETE FROM Settings;
GO
INSERT INTO Settings (SettingKey, SettingValue, Description) VALUES
('GoogleMapsApiKey', '', 'API Key for Google Maps integrations (leave blank to fallback to OpenStreetMap/Leaflet.js)'),
('HuggingFaceApiKey', '', 'Inference API Key for Hugging Face Waste Image Classification'),
('LocalMapsFallback', 'True', 'Allow automatic fallback to Leaflet.js if Google Maps Key is empty');
GO

-- Seed Eco Tips
DELETE FROM EcoTips;
GO
INSERT INTO EcoTips (Title, TipContent, Category, CreatedAt) VALUES
('Rinse Before Recycling', 'Always rinse containers, jars, and bottles before tossing them in the recycling bin. Residual food particles contaminate entire batches of waste.', 'Plastic & Glass', GETDATE()),
('E-Waste Safety', 'Never dispose of batteries or electronic components in standard bins. They contain toxic chemicals that leak into landfills. Find an electronics drop-off center.', 'Electronics', GETDATE()),
('Say No to Plastic Bags', 'Plastic shopping bags are a major cause of recycling machinery jams. Opt for canvas or reusable bags instead, and recycle plastic bags separately.', 'General', GETDATE()),
('Compost Organic Waste', 'Starting a backyard compost pile with fruit peels, coffee grounds, and yard clippings reduces methane emissions and produces nutrient-rich soil.', 'Organic', GETDATE());
GO

-- Seed News
DELETE FROM News;
GO
INSERT INTO News (Title, Content, ImageUrl, CreatedAt) VALUES
('EcoRecycle Launch Event', 'Today marks the official launch of the EcoRecycle platform. Users can now sign up, track nearby recycling centers, schedule pickups, earn reward points, and utilize our AI image classification tool to identify materials. Our mission is to democratize recycling and incentivize positive daily choices.', '/images/news1.jpg', GETDATE()),
('Global E-Waste Challenges in 2026', 'Electronic waste continues to rise exponentially due to short device lifecycles and high consumer demand. Experts estimate that less than 20% of e-waste is properly recycled, leaving valuable materials like gold, silver, and copper unrecovered while hazardous substances pollute soils. EcoRecycle offers specialized electronics categories to address this.', '/images/news2.jpg', GETDATE()),
('Understanding the Plastic Coding System', 'Have you ever noticed the tiny triangular numbers on plastic packaging? They indicate the type of resin used. Number 1 (PETE) and Number 2 (HDPE) are the most widely recycled plastics. Type 3 (PVC) and Type 6 (PS) are generally harder to process. Clean your plastics before scheduling your next EcoRecycle pickup!', '/images/news3.jpg', GETDATE());
GO

-- Seed Rewards
DELETE FROM Rewards;
GO
INSERT INTO Rewards (Name, Description, PointsCost, StockCount, IsActive, ImageUrl) VALUES
('Amazon Gift Card - Rs. 5', 'Redeem 10 points to buy a Rs. 5 Amazon Pay wallet gift card.', 10, 999, 1, '/images/rewards/amazon.png'),
('Amazon Gift Card - Rs. 10', 'Redeem 20 points to buy a Rs. 10 Amazon Pay wallet gift card.', 20, 999, 1, '/images/rewards/amazon.png'),
('Amazon Gift Card - Rs. 25', 'Redeem 50 points to buy a Rs. 25 Amazon Pay wallet gift card.', 50, 999, 1, '/images/rewards/amazon.png'),
('Amazon Gift Card - Rs. 50', 'Redeem 100 points to buy a Rs. 50 Amazon Pay wallet gift card.', 100, 999, 1, '/images/rewards/amazon.png');
GO

-- Seed Campaigns
DELETE FROM Campaigns;
GO
INSERT INTO Campaigns (Name, Description, TargetGoal, CurrentProgress, StartDate, EndDate, IsActive, CreatedAt) VALUES
('Community Plastic Drive 2026', 'Focusing on collecting and processing plastic waste from rivers, beaches, and neighborhoods. Join to earn recycling points.', 1000.00, 0.00, GETDATE(), DATEADD(month, 2, GETDATE()), 1, GETDATE()),
('Electronics Cleanout Month', 'Gather unused, outdated electronics. Let us collect and process old laptops, chargers, and keyboards safely.', 500.00, 0.00, GETDATE(), DATEADD(month, 1, GETDATE()), 1, GETDATE());
GO
