const admin = require('firebase-admin');

if (!admin.apps.length) {
  const serviceAccount = require('../serviceAccountKey.json');
  admin.initializeApp({
    credential: admin.credential.cert(serviceAccount),
    databaseURL: "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/" 
  });
}

module.exports = async (req, res) => {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method Not Allowed' });
  }

  try {
    const { sessionId, userId, deviceId, poiId, language, playTime, durationListened, completionRate, platform, appVersion } = req.body;

    if (!sessionId || !poiId) {
      return res.status(400).json({ error: 'Thiếu dữ liệu bắt buộc (sessionId, poiId)' });
    }

    const db = admin.database();
    
    // 1. Handle GuestSession if no userId
    let guestSessionId = null;
    if (!userId && deviceId) {
      // Find or create guest session
      const guestRef = db.ref('GuestSession');
      const snapshot = await guestRef.orderByChild('deviceId').equalTo(deviceId).once('value');
      
      if (snapshot.exists()) {
        const guests = snapshot.val();
        guestSessionId = Object.keys(guests)[0]; // get the first matching ID
        // Update lastSeenAt
        await db.ref(`GuestSession/${guestSessionId}`).update({
          lastSeenAt: new Date().toISOString()
        });
      } else {
        // Create new guest
        const newGuestRef = guestRef.push();
        await newGuestRef.set({
          deviceId: deviceId,
          platform: platform || 'unknown',
          appVersion: appVersion || '1.0',
          createdAt: new Date().toISOString(),
          lastSeenAt: new Date().toISOString(),
          convertedToUserId: null
        });
        guestSessionId = newGuestRef.key;
      }
    }

    // 2. Log Audio Play
    const audioRef = db.ref('AudioPlayLog').push();
    await audioRef.set({
      playTime: playTime,
      durationListened: durationListened,
      completionRate: completionRate,
      userId: userId || null,
      guestSessionId: guestSessionId,
      sessionId: sessionId,
      poiId: poiId,
      language: language || 'vi'
    });

    res.status(200).json({ status: 'success', message: 'Đã lưu AudioPlayLog thành công' });

  } catch (error) {
    console.error('Lỗi hệ thống Worker mảng AudioPlayLog:', error);
    res.status(500).json({ error: 'Internal Server Error' });
  }
};
