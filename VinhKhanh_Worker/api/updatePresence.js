const admin = require('firebase-admin');

// 1. Khởi tạo Firebase Admin (Chỉ khởi tạo 1 lần)
if (!admin.apps.length) {
  const serviceAccount = require('../serviceAccountKey.json');
  admin.initializeApp({
    credential: admin.credential.cert(serviceAccount),
    // Link Realtime Database của bạn
    databaseURL: "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/" 
  });
}

// 2. Export hàm xử lý (Chuẩn Serverless của Vercel)
module.exports = async (req, res) => {
  // Chỉ nhận Request dạng POST từ App MAUI
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method Not Allowed' });
  }

  try {
    const payload = req.body;
    const { sessionId, userId, deviceId, poiId, latitude, longitude, platform, appVersion } = payload;

    // Kiểm tra dữ liệu đầu vào
    if (!sessionId || !latitude || !longitude) {
      return res.status(400).json({ error: 'Thiếu dữ liệu bắt buộc (sessionId, latitude, longitude)' });
    }

    const timestamp = Date.now(); // Lấy thời gian hiện tại của Server

    // 3. Ghi vào Firebase Realtime Database
    const dbRef = admin.database().ref(`UserPresence/${sessionId}`);
    await dbRef.set({
      userId: userId || null,
      deviceId: deviceId || 'unknown',
      poiId: poiId || null,
      latitude: latitude,
      longitude: longitude,
      updatedAt: timestamp,
      platform: platform || 'MAUI',
      appVersion: appVersion || '1.0'
    });

    // 4. Trả kết quả về cho App MAUI
    res.status(200).json({ status: 'success', message: 'Đã lưu Presence trên Firebase thành công' });

  } catch (error) {
    console.error('Lỗi hệ thống Worker:', error);
    res.status(500).json({ error: 'Internal Server Error' });
  }
};