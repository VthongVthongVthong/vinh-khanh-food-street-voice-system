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
    const { sessionId, userId, deviceId, tourId, language, status, totalPOIs, visitedPOIs } = req.body;

    if (!sessionId || !tourId) {
      return res.status(400).json({ error: 'Thiếu dữ liệu bắt buộc' });
    }

    const db = admin.database();

    // 1. Phân giải GuestSession
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
        // Tạo guest mới tự động (giống logVisit)
        const getNextId = async () => {
          const snap = await db.ref('GuestSession').orderByKey().limitToLast(1).once('value');
          if (snap.exists()) {
            return parseInt(Object.keys(snap.val())[0], 10) + 1;
          }
          return 1;
        };
        guestSessionId = await getNextId();
        await db.ref(`GuestSession/${guestSessionId}`).set({
          id: guestSessionId,
          deviceId: deviceId,
          createdAt: new Date().toISOString(),
          lastSeenAt: new Date().toISOString(),
          convertedToUserId: null
        });
      }
    }

    // 2. Tìm TourLog hiện tại bằng sessionId và tourId
    const tourLogRef = db.ref('TourLog');
    const logsSnapshot = await tourLogRef.orderByChild('sessionId').equalTo(sessionId).once('value');
    
    let existingKey = null;
    if (logsSnapshot.exists()) {
        const logs = logsSnapshot.val();
        for (const [key, log] of Object.entries(logs)) {
            if (log.tourId == tourId) {
                existingKey = key;
                break;
            }
        }
    }

    const completionRate = totalPOIs > 0 ? (visitedPOIs / totalPOIs) * 100 : 0;
    const now = new Date().toISOString();

    if (existingKey) {
        // Update TourLog
        const updates = {
            visitedPOIs: visitedPOIs,
            completionRate: completionRate,
            currentPOIOrder: visitedPOIs,
            status: status,
            updatedAt: now
        };
        if (status === 'completed' || status === 'abandoned') {
            updates.endTime = now;
        }
        await db.ref(`TourLog/${existingKey}`).update(updates);
        return res.status(200).json({ status: 'success', message: 'Updated TourLog', id: existingKey });
    } else {
        // Create new TourLog
        const getNextId = async () => {
          const snap = await db.ref('TourLog').orderByKey().limitToLast(1).once('value');
          if (snap.exists()) {
            return parseInt(Object.keys(snap.val())[0], 10) + 1 || 1;
          }
          return 1;
        };
        const nextId = await getNextId();
        
        await db.ref(`TourLog/${nextId}`).set({
            id: nextId,
            tourId: tourId,
            userId: userId || null,
            guestSessionId: guestSessionId,
            sessionId: sessionId,
            language: language || 'vi',
            startTime: now,
            endTime: null, // Chưa xong
            status: status || 'ongoing',
            totalPOIs: totalPOIs || 0,
            visitedPOIs: visitedPOIs || 0,
            completionRate: completionRate,
            currentPOIOrder: 0,
            createdAt: now,
            updatedAt: now
        });
        return res.status(200).json({ status: 'success', message: 'Created TourLog', id: nextId });
    }

  } catch (error) {
    console.error('Lỗi hệ thống Worker TourLog:', error);
    res.status(500).json({ error: 'Internal Server Error' });
  }
};
