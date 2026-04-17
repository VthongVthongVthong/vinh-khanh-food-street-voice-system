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
    
    // Helpper để tạo ID tăng dần (1, 2, 3...)
    const getNextId = async (nodePath) => {
      const snapshot = await db.ref(nodePath).orderByKey().limitToLast(1).once('value');
      if (snapshot.exists()) {
        const lastKey = Object.keys(snapshot.val())[0];
        const nextId = parseInt(lastKey, 10);
        if (!isNaN(nextId)) {
          return nextId + 1;
        }
      }
      return 1;
    };

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
        guestSessionId = await getNextId('GuestSession');
        await db.ref(`GuestSession/${guestSessionId}`).set({
          id: guestSessionId,
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
    const visitId = await getNextId('VisitLog');
    await db.ref(`VisitLog/${visitId}`).set({
      id: visitId,
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
