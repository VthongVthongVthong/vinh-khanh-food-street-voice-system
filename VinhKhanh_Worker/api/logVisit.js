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
    const { sessionId, userId, deviceId, poiId, visitTime, exitTime, durationStayed, latitude, longitude, triggerType, platform, appVersion } = req.body;

    if (!sessionId || !poiId) {
      return res.status(400).json({ error: 'Thiếu dữ liệu bắt buộc (sessionId, poiId)' });
    }

    const db = admin.database();
    
    // 1. Handle GuestSession if no userId
    let guestSessionId = null;
    if (!userId && deviceId) {
      const guestRef = db.ref('GuestSession');
      const snapshot = await guestRef.orderByChild('deviceId').equalTo(deviceId).once('value');
      
      if (snapshot.exists()) {
        const guests = snapshot.val();
        guestSessionId = Object.keys(guests)[0];
        await db.ref(`GuestSession/${guestSessionId}`).update({
          lastSeenAt: new Date().toISOString()
        });
      } else {
        const newGuestRef = guestRef.push();
        guestSessionId = newGuestRef.key;
        await newGuestRef.set({
          deviceId: deviceId,
          platform: platform || 'unknown',
          appVersion: appVersion || '1.0',
          createdAt: new Date().toISOString(),
          lastSeenAt: new Date().toISOString(),
          convertedToUserId: null
        });
      }
    }

    // 2. Log Visit
    const visitRef = db.ref('VisitLog').push();
    await visitRef.set({
      visitTime: visitTime,
      exitTime: exitTime,
      durationStayed: durationStayed,
      latitude: latitude,
      longitude: longitude,
      userId: userId || null,
      guestSessionId: guestSessionId,
      sessionId: sessionId,
      triggerType: triggerType || 'AUTO',
      poiId: poiId
    });

    res.status(200).json({ status: 'success', message: 'Đã lưu VisitLog thành công' });

  } catch (error) {
    console.error('Lỗi hệ thống Worker mảng VisitLog:', error);
    res.status(500).json({ error: 'Internal Server Error' });
  }
};
