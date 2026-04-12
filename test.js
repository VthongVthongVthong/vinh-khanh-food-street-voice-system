        ﻿        const poiDetailModal = document.getElementById('poiDetailModal');
        let currentPoiData = null;

        function openPoiModal(poi) {
            currentPoiData = poi;
            
            // Lấy thông tin cơ bản
            document.getElementById('modalName').textContent = poi.Name || poi.name || "Không tên";
            document.getElementById('modalAddress').innerHTML = `<i class="fas fa-map-marker-alt text-brand-500 mt-1"></i> <span>${poi.Address || poi.address || 'Đang cập nhật'}</span>`;
            document.getElementById('modalPhone').textContent = poi.Phone || poi.phone || "Không có SĐT";
            document.getElementById('modalRadius').textContent = parseInt(poi.triggerRadiusMeters || poi.triggerradiusmeters || poi.triggerRadiusmeters || 0);
            
            const isActive = parseInt(poi.IsActive ?? poi.isactive ?? poi.isActive ?? 0) === 1;
            document.getElementById('modalStatusBadge').innerHTML = isActive 
                ? `<span class="bg-green-500 text-white text-xs font-bold px-3 py-1.5 rounded-full uppercase tracking-wider shadow-sm flex items-center gap-1.5"><span class="w-1.5 h-1.5 bg-white rounded-full animate-pulse"></span> Hoạt động</span>`
                : `<span class="bg-gray-500 text-white text-xs font-bold px-3 py-1.5 rounded-full uppercase tracking-wider shadow-sm">Tạm ngưng</span>`;

            // Thống kê
            document.getElementById('modalVisits').textContent = poi.visitCount || poi.visitcount || 0;
            document.getElementById('modalAudioPlays').textContent = poi.audioPlayCount || poi.audioplaycount || 0;
            const avgDuration = parseFloat(poi.avgAudioDuration || poi.avgaudioduration || 0);
            document.getElementById('modalAvgDuration').textContent = isNaN(avgDuration) ? "0.0" : avgDuration.toFixed(1);

            // Mã QR
            const poiId = poi.Id || poi.id;
            const qrData = encodeURIComponent(`vinhkhanh://poi?id=${poiId}&action=play`);
            document.getElementById('modalQrCode').src = `https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${qrData}`;

            // Đường dẫn map
            const mapLinkEl = document.getElementById('modalMapLink');
            const link = poi.MapLink || poi.maplink || poi.mapLink || '';
            if (link) {
                mapLinkEl.href = link;
                mapLinkEl.style.display = 'flex';
            } else {
                mapLinkEl.style.display = 'none';
            }

            // Xử lý hình ảnh
            let avatarUrl = poi.avatarUrl || poi.avatarurl || '';
            let bannerUrl = poi.bannerUrl || poi.bannerurl || '';

            // Nếu không có banner/avatar từ POIImage, fallback về ImageUrls
            if (!avatarUrl || !bannerUrl) {
                const rawImageUrls = poi.ImageUrls || poi.imageurls || '[]';
                try {
                    const parsed = JSON.parse(rawImageUrls);
                    if (Array.isArray(parsed)) {
                        if (!avatarUrl && parsed.length > 0) avatarUrl = parsed[0];
                        if (!bannerUrl && parsed.length > 1) bannerUrl = parsed[1];
                    }
                } catch (e) {
                    if (typeof rawImageUrls === 'string' && rawImageUrls.length > 5 && !avatarUrl) avatarUrl = rawImageUrls;
                }
            }

            if (avatarUrl) document.getElementById('modalAvatar').src = avatarUrl;
            if (bannerUrl) document.getElementById('modalBanner').src = bannerUrl;

            // Render ngôn ngữ
            renderLanguageTabs(poi);

            // Hiển thị modal
            poiDetailModal.classList.remove('hidden');
            poiDetailModal.classList.add('flex');
            
            // Animation
            requestAnimationFrame(() => {
                poiDetailModal.classList.remove('opacity-0', 'translate-y-4');
                poiDetailModal.classList.add('opacity-100', 'translate-y-0');
            });
        }

        function closePoiModal() {
            poiDetailModal.classList.remove('opacity-100', 'translate-y-0');
            poiDetailModal.classList.add('opacity-0', 'translate-y-4');
            setTimeout(() => {
                poiDetailModal.classList.remove('flex');
                poiDetailModal.classList.add('hidden');
                
                // Clear state
                activePopup = null;
                currentPoiData = null;
                document.getElementById('modalBanner').src = "";
                document.getElementById('modalAvatar').src = "";
            }, 300); // 300ms is the transition duration
        }

        // Đóng modal khi click ra ngoài vùng xám
        poiDetailModal.addEventListener('click', function(e) {
            if (e.target === this) {
                closePoiModal();
            }
        });

        // Hàm render Tab và Content ngôn ngữ
        function renderLanguageTabs(poi) {
            const tabsEl = document.getElementById('langTabs');
            const contEl = document.getElementById('langContent');
            tabsEl.innerHTML = '';
            contEl.innerHTML = '';

            const languages = [
                { id: 'en', label: 'Anh', keyDesc: 'En', keyTts: 'En', flag: '🇬🇧' },
                { id: 'zh', label: 'Trung', keyDesc: 'Zh', keyTts: 'Zh', flag: '🇨🇳' },
                { id: 'ja', label: 'Nhật', keyDesc: 'Ja', keyTts: 'Ja', flag: '🇯🇵' },
                { id: 'ko', label: 'Hàn', keyDesc: 'Ko', keyTts: 'Ko', flag: '🇰🇷' },
                { id: 'fr', label: 'Pháp', keyDesc: 'Fr', keyTts: 'Fr', flag: '🇫🇷' },
                { id: 'ru', label: 'Nga', keyDesc: 'Ru', keyTts: 'Ru', flag: '🇷🇺' }
            ];

            // Tab Tiếng Việt (Luôn có)
            let hasAtLeastOneExtra = false;
            
            let tabsHtml = `<button onclick="switchTab('vi')" id="tab_vi" class="px-5 py-3.5 text-sm font-medium whitespace-nowrap border-b-2 bg-white border-brand-500 text-brand-600 focus:outline-none transition-colors w-1/3 md:w-auto text-center shrink-0">
                               🇻🇳 Tiếng Việt
                            </button>`;
                            
            const descVi = poi.DescriptionText || poi.descriptionText || poi.descriptiontext || '';
            const ttsVi = poi.TtsScript || poi.ttsScript || poi.ttsscript || '';
            
            let contentHtml = `<div id="content_vi" class="lang-content-panel block space-y-4">
                <div>
                    <h5 class="text-sm font-bold text-gray-700 mb-2 uppercase tracking-tight opacity-80"><i class="fas fa-align-left text-brand-400 mr-2"></i>Mô tả tóm tắt</h5>
                    <div class="bg-gray-50/50 p-4 rounded-xl text-gray-700 border border-gray-100 text-[15px] leading-relaxed">${descVi.replace(/\\n/g, '<br>') || '<i class="text-gray-400">Chưa cập nhật</i>'}</div>
                </div>
                <div>
                    <div class="flex items-center justify-between mb-2">
                        <h5 class="text-sm font-bold text-gray-700 uppercase tracking-tight opacity-80"><i class="fas fa-headphones text-purple-400 mr-2"></i>Kịch bản TTS</h5>
                        ${(ttsVi.trim() || descVi.trim()) ? `<button data-text="${encodeURIComponent(ttsVi.trim() || descVi.trim())}" onclick="playTTS('vi', decodeURIComponent(this.getAttribute('data-text')))" class="text-xs bg-purple-100 text-purple-600 hover:bg-purple-200 px-3 py-1.5 rounded-full font-medium transition-colors flex items-center gap-1.5"><i class="fas fa-play"></i> Nghe</button>` : ''}
                    </div>
                    <div class="bg-purple-50/30 p-4 rounded-xl text-gray-700 border border-purple-100 text-[15px] leading-relaxed">${ttsVi.replace(/\\n/g, '<br>') || '<i class="text-gray-400">Chưa cập nhật</i>'}</div>
                </div>
            </div>`;

            // Kiểm tra các ngôn ngữ khác
            languages.forEach(lang => {
                const descMapCased = 'description' + lang.keyDesc;
                const descPascal = 'Description' + lang.keyDesc;
                const descLower = descMapCased.toLowerCase();
                const descRaw = poi[descMapCased] || poi[descPascal] || poi[descLower] || '';
                
                const ttsMapCased = 'ttsScript' + lang.keyTts;
                const ttsPascal = 'TtsScript' + lang.keyTts;
                const ttsLower = ttsMapCased.toLowerCase();
                const ttsRaw = poi[ttsMapCased] || poi[ttsPascal] || poi[ttsLower] || '';

                if (descRaw.trim() !== '' || ttsRaw.trim() !== '') {
                    hasAtLeastOneExtra = true;
                    tabsHtml += `<button onclick="switchTab('${lang.id}')" id="tab_${lang.id}" class="px-5 py-3.5 text-sm font-medium whitespace-nowrap border-b-2 border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 focus:outline-none transition-colors w-1/3 md:w-auto text-center shrink-0">
                                   ${lang.flag} Tiếng ${lang.label}
                                </button>`;
                    
                    contentHtml += `<div id="content_${lang.id}" class="lang-content-panel hidden space-y-4">
                        <div>
                            <h5 class="text-sm font-bold text-gray-700 mb-2 uppercase tracking-tight opacity-80"><i class="fas fa-align-left text-brand-400 mr-2"></i>Mô tả tóm tắt</h5>
                            <div class="bg-gray-50/50 p-4 rounded-xl text-gray-700 border border-gray-100 text-[15px] leading-relaxed">${descRaw.replace(/\\n/g, '<br>') || '<i class="text-gray-400">Chưa cập nhật</i>'}</div>
                        </div>
                        <div>
                            <div class="flex items-center justify-between mb-2">
                                <h5 class="text-sm font-bold text-gray-700 uppercase tracking-tight opacity-80"><i class="fas fa-headphones text-purple-400 mr-2"></i>Kịch bản TTS</h5>
                                ${(ttsRaw.trim() || descRaw.trim()) ? `<button data-text="${encodeURIComponent(ttsRaw.trim() || descRaw.trim())}" onclick="playTTS('${lang.id}', decodeURIComponent(this.getAttribute('data-text')))" class="text-xs bg-purple-100 text-purple-600 hover:bg-purple-200 px-3 py-1.5 rounded-full font-medium transition-colors flex items-center gap-1.5"><i class="fas fa-play"></i> Nghe</button>` : ''}
                            </div>
                            <div class="bg-purple-50/30 p-4 rounded-xl text-gray-700 border border-purple-100 text-[15px] leading-relaxed">${ttsRaw.replace(/\\n/g, '<br>') || '<i class="text-gray-400">Chưa cập nhật</i>'}</div>
                        </div>
                    </div>`;
                }
            });

            tabsEl.innerHTML = tabsHtml;
            contEl.innerHTML = contentHtml;
        }

        // Đổi tab ngôn ngữ
        window.switchTab = function(tabId) {
            // Reset all tabs
            document.querySelectorAll('#langTabs button').forEach(btn => {
                btn.classList.remove('bg-white', 'border-brand-500', 'text-brand-600');
                btn.classList.add('border-transparent', 'text-gray-500');
            });
            
            // Hide all content
            document.querySelectorAll('.lang-content-panel').forEach(panel => {
                panel.classList.add('hidden');
                panel.classList.remove('block');
            });

            // Active current tab
            const activeTab = document.getElementById('tab_' + tabId);
            if(activeTab) {
                activeTab.classList.remove('border-transparent', 'text-gray-500');
                activeTab.classList.add('bg-white', 'border-brand-500', 'text-brand-600');
            }

            // Show current content
            const activeContent = document.getElementById('content_' + tabId);
            if(activeContent) {
                activeContent.classList.remove('hidden');
                activeContent.classList.add('block');
            }
        };



        let addMap, editMap;
        let addMarker, editMarker;

        // Khởi tạo TrackAsia Map cho modal
        function initModalMaps() {
            // Map Add
            addMap = new trackasiagl.Map({
                container: 'add_map_canvas',
                style: 'https://maps.track-asia.com/styles/v2/streets.json?key=bca01773651908dcc9bc6320f7c16973ce',
                center: [106.702197, 10.761756], // Vĩnh Khánh mặc định
                zoom: 16
            });
            addMap.addControl(new trackasiagl.NavigationControl({showCompass: false}), 'top-right');
            addMarker = new trackasiagl.Marker({color: "#dc2626"})
                .setLngLat([106.702197, 10.761756])
                .addTo(addMap);
            addMap.on('click', (e) => {
                const lng = parseFloat(e.lngLat.lng.toFixed(6));
                const lat = parseFloat(e.lngLat.lat.toFixed(6));
                addMarker.setLngLat([lng, lat]);
                document.getElementById('add_longitude').value = lng;
                document.getElementById('add_latitude').value = lat;
            });

            // Map Edit
            editMap = new trackasiagl.Map({
                container: 'edit_map_canvas',
                style: 'https://maps.track-asia.com/styles/v2/streets.json?key=bca01773651908dcc9bc6320f7c16973ce',
                center: [106.702197, 10.761756],
                zoom: 16
            });
            editMap.addControl(new trackasiagl.NavigationControl({showCompass: false}), 'top-right');
            editMarker = new trackasiagl.Marker({color: "#dc2626"})
                .setLngLat([106.702197, 10.761756])
                .addTo(editMap);
            editMap.on('click', (e) => {
                const lng = parseFloat(e.lngLat.lng.toFixed(6));
                const lat = parseFloat(e.lngLat.lat.toFixed(6));
                editMarker.setLngLat([lng, lat]);
                document.getElementById('edit_longitude').value = lng;
                document.getElementById('edit_latitude').value = lat;
            });
        }
        
        // Gọi init lúc DOM ready
        window.addEventListener('DOMContentLoaded', () => {
            initModalMaps();
        });

        let currentAudioMap = null;

        // Cấu hình API Keys
        const ELEVEN_LABS_API_KEY = "sk_8db9fdb8efa165866e85ccc071c5ef803364bbd93324fe05";
        
        function base64ToArrayBuffer(base64) {
            const binaryString = atob(base64);
            const len = binaryString.length;
            const bytes = new Uint8Array(len);
            for (let i = 0; i < len; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }
            return bytes;
        }

        // 🔥 tạo WAV header
        function createWavFile(pcmData, sampleRate = 24000) {
            const buffer = new ArrayBuffer(44 + pcmData.length);
            const view = new DataView(buffer);

            function writeString(view, offset, str) {
                for (let i = 0; i < str.length; i++) {
                    view.setUint8(offset + i, str.charCodeAt(i));
                }
            }

            writeString(view, 0, 'RIFF');
            view.setUint32(4, 36 + pcmData.length, true);
            writeString(view, 8, 'WAVE');
            writeString(view, 12, 'fmt ');
            view.setUint32(16, 16, true); // PCM chunk size
            view.setUint16(20, 1, true); // PCM format
            view.setUint16(22, 1, true); // mono
            view.setUint32(24, sampleRate, true);
            view.setUint32(28, sampleRate * 2, true); // byte rate
            view.setUint16(32, 2, true); // block align
            view.setUint16(34, 16, true); // bits per sample
            writeString(view, 36, 'data');
            view.setUint32(40, pcmData.length, true);

            const wavBytes = new Uint8Array(buffer);
            wavBytes.set(pcmData, 44);

            return new Blob([wavBytes], { type: "audio/wav" });
        }

        async function playTTS(lang, text) {
            if (!text) {
                alert("Không có nội dung để đọc.");
                return;
            }

            // Nếu đang đọc dở thì dừng
            stopTTS();

            try {
                // Hiển thị toast thông báo
                const toast = document.createElement('div');
                toast.id = 'tts_toast_loading';
                toast.className = 'fixed bottom-4 right-4 bg-blue-600 text-white px-4 py-2 rounded-lg shadow-lg z-50 text-sm flex items-center gap-2';
                toast.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang gọi API TTS...';
                document.body.appendChild(toast);

                let audioUrl;

                if (lang === 'vi') {
                    // Dùng Gemini TTS cho tiếng Việt
                    const response = await fetch(
                        "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-tts:generateContent?key=",
                        {
                            method: "POST",
                            headers: { "Content-Type": "application/json" },
                            body: JSON.stringify({
                                contents: [{ parts: [{ text: text }] }],
                                generationConfig: { responseModalities: ["AUDIO"] }
                            })
                        }
                    );

                    const data = await response.json();

                    if (!data.candidates) {
                        console.error("Gemini TTS Error:", data);
                        document.getElementById('tts_toast_loading')?.remove();
                        alert("API lỗi: " + (data.error?.message || "Unknown"));
                        return;
                    }

                    const base64 = data.candidates[0].content.parts[0].inlineData.data;
                    const pcmData = base64ToArrayBuffer(base64);
                    const wavBlob = createWavFile(pcmData);
                    audioUrl = URL.createObjectURL(wavBlob);

                } else {
                    // Dùng ElevenLabs cho các ngôn ngữ khác
                    const voiceId = "EXAVITQu4vr4xnSDxMaL";
                    const response = await fetch(`https://api.elevenlabs.io/v1/text-to-speech/${voiceId}`, {
                        method: 'POST',
                        headers: {
                            'xi-api-key': ELEVEN_LABS_API_KEY,
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({
                            text: text,
                            model_id: "eleven_multilingual_v2"
                        })
                    });

                    if (!response.ok) {
                        const errText = await response.text();
                        console.error("ElevenLabs Error:", errText);
                        document.getElementById('tts_toast_loading')?.remove();
                        alert("Lỗi kết nối ElevenLabs API: " + errText);
                        return;
                    }

                    const audioBlob = await response.blob();
                    audioUrl = URL.createObjectURL(audioBlob);
                }

                // Phát âm thanh chung
                currentAudioMap = new Audio(audioUrl);
                currentAudioMap.play();
                
                document.getElementById('tts_toast_loading').innerHTML = '<i class="fas fa-volume-up"></i> Đang đọc...';
                
                currentAudioMap.onended = () => {
                    document.getElementById('tts_toast_loading')?.remove();
                    URL.revokeObjectURL(audioUrl);
                };
                
                currentAudioMap.onerror = () => {
                    document.getElementById('tts_toast_loading')?.remove();
                    alert("Không thể phát file âm thanh.");
                };

            } catch (err) {
                console.error('Fetch Error:', err);
                document.getElementById('tts_toast_loading')?.remove();
                alert("Lỗi gọi API TTS.");
            }
        }

        function stopTTS() {
            // Dừng đối tượng âm thanh MP3
            if (currentAudioMap) {
                currentAudioMap.pause();
                currentAudioMap.currentTime = 0;
            }
            
            document.getElementById('tts_toast_loading')?.remove();

            // Đảm bảo dừng cả Web Speech API cũ (trường hợp bị xung đột)
            if ('speechSynthesis' in window) {
                window.speechSynthesis.cancel();
            }
        }

        function openEditModal(rawPoi) {
            const form = document.getElementById('editPoiForm');
            form.submitted = false; // Reset trạng thái submit
            // Chuyển toàn bộ keys về chữ thường để lấy data chính xác bất kể MySQL trả về kiểu gì
            const poi = {};
            for (let key in rawPoi) {
                poi[key.toLowerCase()] = rawPoi[key];
            }

            document.getElementById('editModal').classList.remove('hidden');
            
            document.getElementById('edit_id').value = poi.id || '';
            document.getElementById('edit_name').value = poi.name || '';
            document.getElementById('edit_ownerId').value = poi.ownerid || '';
            document.getElementById('edit_latitude').value = poi.latitude || '0';
            document.getElementById('edit_longitude').value = poi.longitude || '0';
            document.getElementById('edit_address').value = poi.address || '';
            document.getElementById('edit_phone').value = poi.phone || '';
            document.getElementById('edit_triggerRadiusMeters').value = poi.triggerradiusmeters || poi.triggerradius || '20';
            document.getElementById('edit_mapLink').value = poi.maplink || '';
            document.getElementById('edit_isActive').value = poi.isactive !== undefined ? poi.isactive : '1';
            
            // Xử lý json ảnh trả ra field (nếu cần thiết)
            let imgs = poi.imageurls || '';
            document.getElementById('edit_imageUrls').value = imgs;

            // Nội dung & Thuyết minh
            document.getElementById('edit_descriptionText').value = poi.descriptiontext || '';
            document.getElementById('edit_descriptionEn').value = poi.descriptionen || '';
            document.getElementById('edit_descriptionZh').value = poi.descriptionzh || '';
            document.getElementById('edit_descriptionJa').value = poi.descriptionja || '';
            document.getElementById('edit_descriptionKo').value = poi.descriptionko || '';
            document.getElementById('edit_descriptionFr').value = poi.descriptionfr || '';
            document.getElementById('edit_descriptionRu').value = poi.descriptionru || '';
            
            document.getElementById('edit_ttsScript').value = poi.ttsscript || '';
            document.getElementById('edit_ttsScriptEn').value = poi.ttsscripten || '';
            document.getElementById('edit_ttsScriptZh').value = poi.ttsscriptzh || '';
            document.getElementById('edit_ttsScriptJa').value = poi.ttsscriptja || '';
            document.getElementById('edit_ttsScriptKo').value = poi.ttsscriptko || '';
            document.getElementById('edit_ttsScriptFr').value = poi.ttsscriptfr || '';
            document.getElementById('edit_ttsScriptRu').value = poi.ttsscriptru || '';

            // Kiểm tra xem đã có dữ liệu tiếng Anh / Trung chưa để hiển thị tương ứng
            if ((poi.descriptionen && poi.descriptionen.trim() !== '') || (poi.ttsscripten && poi.ttsscripten.trim() !== '')) {
                showEditLang('en');
            } else {
                hideEditLang('en');
            }

            if ((poi.descriptionzh && poi.descriptionzh.trim() !== '') || (poi.ttsscriptzh && poi.ttsscriptzh.trim() !== '')) {
                showEditLang('zh');
            } else {
                hideEditLang('zh');
            }

            if ((poi.descriptionja && poi.descriptionja.trim() !== '') || (poi.ttsscriptja && poi.ttsscriptja.trim() !== '')) {
                showEditLang('ja');
            } else {
                hideEditLang('ja');
            }

            if ((poi.descriptionko && poi.descriptionko.trim() !== '') || (poi.ttsscriptko && poi.ttsscriptko.trim() !== '')) {
                showEditLang('ko');
            } else {
                hideEditLang('ko');
            }

            if ((poi.descriptionfr && poi.descriptionfr.trim() !== '') || (poi.ttsscriptfr && poi.ttsscriptfr.trim() !== '')) {
                showEditLang('fr');
            } else {
                hideEditLang('fr');
            }

            if ((poi.descriptionru && poi.descriptionru.trim() !== '') || (poi.ttsscriptru && poi.ttsscriptru.trim() !== '')) {
                showEditLang('ru');
            } else {
                hideEditLang('ru');
            }

            // Prevent scrolling on body
            document.body.style.overflow = 'hidden';

            // Cập nhật bản đồ edit
            setTimeout(() => {
                if (editMap) {
                    editMap.resize();
                    const lat = parseFloat(poi.latitude) || 10.761756;
                    const lng = parseFloat(poi.longitude) || 106.702197;
                    editMap.setCenter([lng, lat]);
                    editMarker.setLngLat([lng, lat]);
                }
            }, 300);
        }

        function closeEditModal() {
            document.getElementById('editModal').classList.add('hidden');
            document.body.style.overflow = 'auto';
        }

        // Close on backdrop click
        document.getElementById('editModal').addEventListener('click', function(e) {
            if (e.target === this) {
                closeEditModal();
            }
        });

        // Tự động dịch bằng LibreTranslate API
        async function autoTranslate() {
            const descVi = document.getElementById('edit_descriptionText').value.trim();
            const ttsVi = document.getElementById('edit_ttsScript').value.trim();

            if (!descVi && !ttsVi) {
                alert("Vui lòng nhập nội dung Tiếng Việt trước khi dịch!");
                return;
            }

            const hasEn = document.getElementById('edit_has_en').value === '1';
            const hasZh = document.getElementById('edit_has_zh').value === '1';
            const hasJa = document.getElementById('edit_has_ja').value === '1';
            const hasKo = document.getElementById('edit_has_ko').value === '1';
            const hasFr = document.getElementById('edit_has_fr').value === '1';
            const hasRu = document.getElementById('edit_has_ru').value === '1';

            if (!hasEn && !hasZh && !hasJa && !hasKo && !hasFr && !hasRu) {
                alert("Vui lòng bật ít nhất một ngôn ngữ để dịch!");
                return;
            }

            const btnText = document.getElementById('translateBtnText');
            btnText.innerText = "Đang dịch...";
            
            try {
                // Hàm gọi API LibreTranslate đúng chuẩn (và API dự phòng nếu LibreTranslate đòi Key)
                const translateText = async (text, targetLang) => {
                    if (!text) return "";
                    
                    try {
                        const res = await fetch("https://libretranslate.com/translate", {
                            method: "POST",
                            body: JSON.stringify({
                                q: text,
                                source: "auto", // Đổi lại thành 'auto' như bạn yêu cầu
                                target: targetLang,
                                format: "text",
                                alternatives: 3,
                                api_key: ""
                            }),
                            headers: { "Content-Type": "application/json" }
                        });
                        
                        if (!res.ok) {
                            const errData = await res.json().catch(() => null);
                            console.error("LibreTranslate Error:", errData);
                            throw new Error("LibreTranslate API bị lỗi (thường do yêu cầu API Key).");
                        }
                        
                        const data = await res.json();
                        return data.translatedText || "";
                        
                    } catch (e) {
                        console.warn("Chuyển sang dùng Google Translate (Backup) vì LibreTranslate bị lỗi:", e.message);
                        
                        // API dự phòng (Google Translate Free) hoạt động 100% khi backend kia khóa
                        const url = `https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl=${targetLang}&dt=t&q=${encodeURIComponent(text)}`;
                        const resBackup = await fetch(url);
                        const dataBackup = await resBackup.json();
                        return dataBackup[0].map(x => x[0]).join('');
                    }
                };

                const promises = [];
                
                if(descVi) {
                    if(hasEn) promises.push(translateText(descVi, "en").then(res => document.getElementById('edit_descriptionEn').value = res));
                    if(hasZh) promises.push(translateText(descVi, "zh").then(res => document.getElementById('edit_descriptionZh').value = res));
                    if(hasJa) promises.push(translateText(descVi, "ja").then(res => document.getElementById('edit_descriptionJa').value = res));
                    if(hasKo) promises.push(translateText(descVi, "ko").then(res => document.getElementById('edit_descriptionKo').value = res));
                    if(hasFr) promises.push(translateText(descVi, "fr").then(res => document.getElementById('edit_descriptionFr').value = res));
                    if(hasRu) promises.push(translateText(descVi, "ru").then(res => document.getElementById('edit_descriptionRu').value = res));
                }
                
                if(ttsVi) {
                    if(hasEn) promises.push(translateText(ttsVi, "en").then(res => document.getElementById('edit_ttsScriptEn').value = res));
                    if(hasZh) promises.push(translateText(ttsVi, "zh").then(res => document.getElementById('edit_ttsScriptZh').value = res));
                    if(hasJa) promises.push(translateText(ttsVi, "ja").then(res => document.getElementById('edit_ttsScriptJa').value = res));
                    if(hasKo) promises.push(translateText(ttsVi, "ko").then(res => document.getElementById('edit_ttsScriptKo').value = res));
                    if(hasFr) promises.push(translateText(ttsVi, "fr").then(res => document.getElementById('edit_ttsScriptFr').value = res));
                    if(hasRu) promises.push(translateText(ttsVi, "ru").then(res => document.getElementById('edit_ttsScriptRu').value = res));
                }

                await Promise.all(promises);

            } catch (error) {
                console.error(error);
                alert("Đã xảy ra lỗi khi dịch. Có thể do API giới hạn lượt gọi hoặc mạng có vấn đề.");
            } finally {
                btnText.innerText = "Dịch tự động";
            }
        }

        // Logic Modal Thêm Mới
        function openAddModal() {
            const form = document.getElementById('addPoiForm');
            form.reset();
            form.submitted = false; // Reset trạng thái submit
            ['en', 'zh', 'ja', 'ko', 'fr', 'ru'].forEach(lang => {
                document.getElementById('add_has_' + lang).value = '1';
                document.getElementById('add_' + lang + '_container').classList.remove('hidden');
            });
            document.getElementById('addModal').classList.remove('hidden');
            document.body.style.overflow = 'hidden';
            
            setTimeout(() => {
                if (addMap) {
                    addMap.resize();
                    const lat = parseFloat(document.getElementById('add_latitude').value) || 10.761756;
                    const lng = parseFloat(document.getElementById('add_longitude').value) || 106.702197;
                    addMap.setCenter([lng, lat]);
                    addMarker.setLngLat([lng, lat]);
                }
            }, 300);
        }

        function closeAddModal() {
            document.getElementById('addModal').classList.add('hidden');
            document.body.style.overflow = 'auto';
        }

        document.getElementById('addModal').addEventListener('click', function(e) {
            if (e.target === this) closeAddModal();
        });

        function showEditLang(lang) {
            document.getElementById('edit_' + lang + '_container').classList.remove('hidden');
            document.getElementById('btn_edit_' + lang).classList.add('hidden');
            document.getElementById('edit_has_' + lang).value = '1';
        }

        function hideEditLang(lang) {
            document.getElementById('edit_' + lang + '_container').classList.add('hidden');
            document.getElementById('btn_edit_' + lang).classList.remove('hidden');
            document.getElementById('edit_has_' + lang).value = '0';
        }
        
        async function autoTranslateAdd() {
            const descVi = document.getElementById('add_descriptionText').value.trim();
            const ttsVi = document.getElementById('add_ttsScript').value.trim();

            if (!descVi && !ttsVi) {
                alert("Vui lòng nhập nội dung Tiếng Việt trước khi dịch!");
                return;
            }

            const hasEn = document.getElementById('add_has_en').value === '1';
            const hasZh = document.getElementById('add_has_zh').value === '1';
            const hasJa = document.getElementById('add_has_ja').value === '1';
            const hasKo = document.getElementById('add_has_ko').value === '1';
            const hasFr = document.getElementById('add_has_fr').value === '1';
            const hasRu = document.getElementById('add_has_ru').value === '1';

            if (!hasEn && !hasZh && !hasJa && !hasKo && !hasFr && !hasRu) {
                alert("Vui lòng bật ít nhất một ngôn ngữ để dịch!");
                return;
            }

            const btnText = document.getElementById('translateBtnTextAdd');
            btnText.innerText = "Đang dịch...";
            
            try {
                // Tái sử dụng logic gọi API
                const translateText = async (text, targetLang) => {
                    if (!text) return "";
                    try {
                        const res = await fetch("https://libretranslate.com/translate", {
                            method: "POST",
                            body: JSON.stringify({
                                q: text,
                                source: "auto",
                                target: targetLang,
                                format: "text",
                                alternatives: 3,
                                api_key: ""
                            }),
                            headers: { "Content-Type": "application/json" }
                        });
                        
                        if (!res.ok) throw new Error("LibreTranslate Error");
                        const data = await res.json();
                        return data.translatedText || "";
                        
                    } catch (e) {
                        const url = `https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl=${targetLang}&dt=t&q=${encodeURIComponent(text)}`;
                        const resBackup = await fetch(url);
                        const dataBackup = await resBackup.json();
                        return dataBackup[0].map(x => x[0]).join('');
                    }
                };

                const promises = [];
                if(descVi) {
                    if(hasEn) promises.push(translateText(descVi, "en").then(res => document.getElementById('add_descriptionEn').value = res));
                    if(hasZh) promises.push(translateText(descVi, "zh").then(res => document.getElementById('add_descriptionZh').value = res));
                    if(hasJa) promises.push(translateText(descVi, "ja").then(res => document.getElementById('add_descriptionJa').value = res));
                    if(hasKo) promises.push(translateText(descVi, "ko").then(res => document.getElementById('add_descriptionKo').value = res));
                    if(hasFr) promises.push(translateText(descVi, "fr").then(res => document.getElementById('add_descriptionFr').value = res));
                    if(hasRu) promises.push(translateText(descVi, "ru").then(res => document.getElementById('add_descriptionRu').value = res));
                }
                if(ttsVi) {
                    if(hasEn) promises.push(translateText(ttsVi, "en").then(res => document.getElementById('add_ttsScriptEn').value = res));
                    if(hasZh) promises.push(translateText(ttsVi, "zh").then(res => document.getElementById('add_ttsScriptZh').value = res));
                    if(hasJa) promises.push(translateText(ttsVi, "ja").then(res => document.getElementById('add_ttsScriptJa').value = res));
                    if(hasKo) promises.push(translateText(ttsVi, "ko").then(res => document.getElementById('add_ttsScriptKo').value = res));
                    if(hasFr) promises.push(translateText(ttsVi, "fr").then(res => document.getElementById('add_ttsScriptFr').value = res));
                    if(hasRu) promises.push(translateText(ttsVi, "ru").then(res => document.getElementById('add_ttsScriptRu').value = res));
                }

                await Promise.all(promises);
            } catch (error) {
                console.error(error);
                alert("Đã xảy ra lỗi khi dịch.");
            } finally {
                btnText.innerText = "Dịch tự động";
            }
        }
    </script>
