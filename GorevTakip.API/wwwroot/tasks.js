const API_BASE_URL = '/api'; 
const token = localStorage.getItem('token');

let allTasks = [];          // Tüm görevleri hafızada tutuyoruz
let currentFilter = 'all';  // Aktif filtre durumu
let currentSearchQuery = ''; // Aktif arama metni
let taskToDeleteId = null;  // Silinecek görevin ID'sini tutar
let currentPage = 1;
const pageSize = 5; // Sayfa başına 5 görev gösterelim
let totalPages = 1;
let currentSortBy = 'duedate'; // Varsayılan sıralama kolonu
let isSortDescending = true;   // Varsayılan sıralama yönü (Yeni eklenenler en üstte)

if (!token) {
    window.location.href = 'index.html';
}

// Token'ı çözen (Decode eden) fonksiyon
function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        return JSON.parse(jsonPayload);
    } catch (e) {
        return null;
    }
}

// Global kullanıcı rolü değişkeni
let userRole = '';
const tokenData = token ? parseJwt(token) : null;
// API'den gelen Claim isimlendirmeleri (URL şeklinde olabilir, bu yüzden kapsayıcı bir atama yapıyoruz)
if (tokenData) {
    userRole = tokenData['role'] || tokenData['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 'User';
}

document.addEventListener('DOMContentLoaded', () => {
    fetchTasks();
    fetchUsers();

    // Silme onay butonuna tıklanma olayını dinle
    const confirmBtn = document.getElementById('confirmDeleteBtn');
    if(confirmBtn) {
        confirmBtn.addEventListener('click', confirmDeleteAction);
    }

    // YENİ: Kullanıcı rolü "Admin" ise Kullanıcı Yönetimi butonunu görünür yap
    if (typeof userRole !== 'undefined' && userRole === 'Admin') {
        const adminBtn = document.getElementById('adminUsersBtn');
        if (adminBtn) {
            // Butonun CSS display özelliğini inline-block (veya flex) yaparak görünür hale getiriyoruz
            adminBtn.style.display = 'inline-block'; 
        }
    }
});

// 1. Tarih Formatlama Yardımcı Fonksiyonu
function formatDate(dateString) {
    if (!dateString) return '-';
    const options = { year: 'numeric', month: 'long', day: 'numeric' };
    return new Date(dateString).toLocaleDateString('tr-TR', options);
}

// 2. Görevleri Listeleme (GET İstemi + Loading Efekti)
async function fetchTasks() {
    const tbody = document.getElementById('tasksTableBody');
    tbody.innerHTML = `<tr><td colspan="5" style="text-align: center; color: #6b7280;"><i class="fa-solid fa-spinner fa-spin"></i> Yükleniyor...</td></tr>`;
    
    // API için sorgu parametrelerini hazırla
    const params = new URLSearchParams({
        PageNumber: currentPage,
        PageSize: pageSize
    });

    if (currentFilter !== 'all') {
        params.append('Status', currentFilter);
    }
    if (currentSearchQuery) {
        params.append('SearchText', currentSearchQuery);
    }
    // YENİ EKLENENLER: API'ye sıralama bilgisini gönderiyoruz
    if (currentSortBy) {
        params.append('SortBy', currentSortBy);
        params.append('SortDescending', isSortDescending);
    }

    try {
        const response = await fetch(`${API_BASE_URL}/Tasks?${params.toString()}`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const result = await response.json();
            
            allTasks = result.data || result.Data || []; 
            totalPages = result.totalPages || result.TotalPages || 1;
            const totalRecords = result.totalRecords || result.TotalRecords || 0;
            
            renderTasks(allTasks);
            renderPagination();
            
            const statTotal = document.getElementById('statTotal');
            if (statTotal) statTotal.textContent = `Bulunan Görev: ${totalRecords}`;

        } else if (response.status === 401) {
            showToast('Oturumunuz süresi dolmuş. Lütfen tekrar giriş yapın.', 'error');
            setTimeout(logout, 1500);
        } else {
            // EKSİK OLAN VE YÜKLENİYOR'DA BIRAKAN KISIM BURASIYDI
            const errText = await response.text();
            console.error("API Hatası:", errText);
            tbody.innerHTML = `<tr><td colspan="5" style="text-align: center; color: #ef4444; font-weight: bold;">Sunucu Hatası: Hata detayını görmek için F12 Konsola bakın.</td></tr>`;
            showToast("Tüm görevler çekilirken sunucuda bir hata oluştu.", "error");
        }
    } catch (error) {
        console.error('Görevler çekilirken hata oluştu:', error);
        tbody.innerHTML = `<tr><td colspan="5" style="text-align: center; color: #ef4444;">Görevler yüklenirken sunucuya ulaşılamadı!</td></tr>`;
    }
}

// 3. Filtreleme ve Arama Mekanizmaları
function filterTasks(status) {
    currentFilter = status;
    currentPage = 1; // Filtre değişince 1. sayfaya dön
    fetchTasks();
}

// Arama için kullanıcı yazmayı bitirene kadar bekleme (Debounce) ekliyoruz
let searchTimeout;
function handleSearch(query) {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => {
        currentSearchQuery = query.toLowerCase().trim();
        currentPage = 1; // Arama yapınca 1. sayfaya dön
        fetchTasks();
    }, 400); // Kullanıcı tuşa basmayı bıraktıktan 400ms sonra istek at
}

// 4. İstatistik Sayaçlarını Güncelleme
function updateStats() {
    const total = allTasks.length;
    const completed = allTasks.filter(t => t.status === 3).length;
    
    const statTotal = document.getElementById('statTotal');
    const statCompleted = document.getElementById('statCompleted');
    
    if (statTotal) statTotal.textContent = `Toplam: ${total}`;
    if (statCompleted) statCompleted.textContent = `Tamamlanan: ${completed}`;
}

// 5. Görevleri Tabloya Yazdırma (Modern Tasarım)
function renderTasks(tasks) {
    const tbody = document.getElementById('tasksTableBody');
    tbody.innerHTML = '';

    if (tasks.length === 0) {
        // DİKKAT: Sütun sayısı 7'ye (Kategori eklendiği için) çıktığı için colspan 7 yapıldı
        tbody.innerHTML = `<tr><td colspan="7" style="text-align: center; color: #6b7280; padding: 20px;">Görev bulunamadı.</td></tr>`;
        return;
    }

    tasks.forEach(task => {
        const tr = document.createElement('tr');
        const taskId = task.id || task.Id;

        // Seçili duruma göre badge renkleri
        let statusClass = "background-color: #e5e7eb; color: #374151;"; // Varsayılan (Yapılacak)
        if (task.status === 2) statusClass = "background-color: #fef08a; color: #854d0e;"; // Devam Ediyor
        if (task.status === 3) statusClass = "background-color: #bbf7d0; color: #166534;"; // Tamamlandı

        let rowStyle = task.status === 3 ? "opacity: 0.7;" : "";
        tr.style = rowStyle;

        // Yetkiye göre butonları hazırlama
        let actionButtons = '';
        
        // Geçmiş butonu HERKES için oluşturulur
        const historyBtn = `
            <button onclick="openHistoryModal(${taskId})" class="action-btn" style="background-color: #6366f1; color: white;" title="Geçmişi Gör">
                <i class="fa-solid fa-clock-rotate-left"></i>
            </button>
        `;
        
        // YENİ: Yorum butonu HERKES için oluşturulur
        const commentBtn = `
            <button onclick="openCommentModal(${taskId})" class="action-btn" style="background-color: #3b82f6; color: white;" title="Yorumlar">
                <i class="fa-regular fa-comments"></i>
            </button>
        `;

        if (userRole === 'Admin') {
            actionButtons = `
                <button onclick="openEditModal(${taskId})" class="action-btn btn-edit" title="Düzenle">
                    <i class="fa-solid fa-pen"></i>
                </button>
                <button onclick="openDeleteModal(${taskId})" class="action-btn btn-delete" title="Sil">
                    <i class="fa-solid fa-trash"></i>
                </button>
                ${historyBtn}
                ${commentBtn}
            `;
        } else {
            actionButtons = `${historyBtn} ${commentBtn}`;
        }

        // --- YENİ EKLENEN: KATEGORİ BADGE MANTIĞI ---
        let categoryLabel = "Belirsiz";
        let catBg = "#f3f4f6", catColor = "#374151";

        switch(task.category) {
            case 1: categoryLabel = "Frontend"; catBg = "#e0f2fe"; catColor = "#0284c7"; break;
            case 2: categoryLabel = "Backend"; catBg = "#ede9fe"; catColor = "#7c3aed"; break;
            case 3: categoryLabel = "Veritabanı"; catBg = "#fce7f3"; catColor = "#db2777"; break;
            case 4: categoryLabel = "BugFix"; catBg = "#fee2e2"; catColor = "#dc2626"; break;
            case 5: categoryLabel = "Mobil"; catBg = "#ffedd5"; catColor = "#ea580c"; break;
            case 6: categoryLabel = "DevOps"; catBg = "#dcfce7"; catColor = "#16a34a"; break;
        }

        const categoryBadge = `<span style="background-color: ${catBg}; color: ${catColor}; padding: 4px 8px; border-radius: 6px; font-size: 12px; font-weight: 600;">${categoryLabel}</span>`;
        // ---------------------------------------------

        // YENİ EKLENEN: Gecikmiş Görev Kontrolü
        const isOverdue = task.isOverdue || task.IsOverdue;
        const dateIcon = isOverdue 
            ? '<i class="fa-solid fa-triangle-exclamation" style="color: #ef4444; margin-right:5px;" title="Gecikmiş Görev!"></i>' 
            : '<i class="fa-regular fa-calendar" style="margin-right:5px; color:#9ca3af;"></i>';

        const dateStyle = isOverdue 
            ? 'color: #ef4444; font-weight: bold; background: #fee2e2; padding: 4px 8px; border-radius: 4px;' 
            : '';

        // TABLO İÇERİĞİ OLUŞTURMA
        tr.innerHTML = `
            <td><strong>${task.title}</strong></td>
            <td style="color: #6b7280;">${task.description || '-'}</td>
            
            <!-- YENİ EKLENEN KISIM: Kategori Sütunu -->
            <td>${categoryBadge}</td>

            <!-- YENİ EKLENEN KISIM: Gecikme Uyarılı Tarih -->
            <td><span style="${dateStyle}">${dateIcon}${formatDate(task.dueDate)}</span></td>
            
            <!-- Atanan Kişi İkonu ve Adı -->
            <td>
                <div style="display: flex; align-items: center; gap: 8px;">
                    <div style="width: 28px; height: 28px; border-radius: 50%; background-color: #3b82f6; color: white; display: flex; justify-content: center; align-items: center; font-size: 12px; font-weight: bold;">
                        ${(task.assignedUserName || 'B').charAt(0).toUpperCase()}
                    </div>
                    <span>${task.assignedUserName || 'Bilinmiyor'}</span>
                </div>
            </td>

            <td>
                <select onchange="updateTaskStatus(${taskId}, this.value)" class="status-select" style="${statusClass}">
                    <option value="1" ${task.status === 1 ? 'selected' : ''}>Yapılacak</option>
                    <option value="2" ${task.status === 2 ? 'selected' : ''}>Devam Ediyor</option>
                    <option value="3" ${task.status === 3 ? 'selected' : ''}>Tamamlandı</option>
                </select>
            </td>
            <td>
                ${actionButtons}
            </td>
        `;
        tbody.appendChild(tr);
    });
}

// 6. Yeni Görev Ekleme (POST + Loading)
async function createTask() {
    const title = document.getElementById('taskTitle').value;
    const description = document.getElementById('taskDescription').value;
    const assignedUserId = document.getElementById('taskAssignedUserId').value;
    const dueDate = document.getElementById('taskDueDate').value;
    
    // YENİ EKLENDİ: Kategori değerini arayüzden alıyoruz
    const category = document.getElementById('taskCategory').value;
    
    const submitBtn = document.querySelector('.btn-success');
    const originalBtnText = submitBtn ? submitBtn.innerText : 'Ekle';

    // YENİ EKLENDİ: category değişkenini de boş mu diye kontrol ediyoruz
    if (!title || !assignedUserId || !category) {
        showToast("Lütfen başlık, atanacak kullanıcı ve kategoriyi seçin.", "error");
        return;
    }

    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerText = "Ekleniyor...";
    }

    try {
        const response = await fetch(`${API_BASE_URL}/Tasks`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ 
                title: title, 
                description: description,
                assignedUserId: parseInt(assignedUserId),
                category: parseInt(category), // YENİ EKLENDİ: Veriyi JSON'a ekliyoruz
                dueDate: dueDate ? new Date(dueDate).toISOString() : null 
            })
        });

        if (response.ok) {
            showToast("Görev başarıyla eklendi!", "success");
            
            // İnputları temizle
            document.getElementById('taskTitle').value = '';
            document.getElementById('taskDescription').value = '';
            document.getElementById('taskAssignedUserId').value = '';
            document.getElementById('taskDueDate').value = '';
            
            // YENİ EKLENDİ: Kayıt başarılı olunca kategori seçimini de sıfırla
            document.getElementById('taskCategory').value = '';
            
            fetchTasks();
        } else {
            const errorText = await response.text();
            showToast(`Görev eklenemedi: ${errorText}`, "error");
        }
    } catch (error) {
        console.error('Sistemsel hata:', error);
        showToast('Sunucu bağlantı hatası.', 'error');
    } finally {
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerText = originalBtnText;
        }
    }
}

// 7. Çıkış Yapma
function logout() {
    localStorage.removeItem('token');
    window.location.href = 'index.html';
}

// 8. Görevi Silme İşlemleri (Özel Modal ile)
function openDeleteModal(taskId) {
    taskToDeleteId = taskId;
    document.getElementById('deleteConfirmModal').style.display = 'flex';
}

function closeDeleteModal() {
    taskToDeleteId = null;
    document.getElementById('deleteConfirmModal').style.display = 'none';
}

async function confirmDeleteAction() {
    if (!taskToDeleteId) return;
    
    const taskId = taskToDeleteId;
    closeDeleteModal(); 

    try {
        const response = await fetch(`${API_BASE_URL}/Tasks/${taskId}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (response.ok) {
            showToast("Görev başarıyla silindi.", "success");
            fetchTasks(); 
        } else {
            showToast('Görev silinirken hata oluştu.', 'error');
        }
    } catch (error) {
        console.error('Silme hatası:', error);
        showToast('Sunucu bağlantı hatası.', 'error');
    }
}

// 9. Görev Durumunu Güncelleme
async function updateTaskStatus(taskId, newStatus) {
    const currentTask = allTasks.find(t => (t.id || t.Id) == taskId);
    if (!currentTask) return;

    try {
        const response = await fetch(`${API_BASE_URL}/Tasks/${taskId}`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ 
                id: parseInt(taskId), 
                title: currentTask.title, 
                description: currentTask.description || "", 
                status: parseInt(newStatus), 
                dueDate: currentTask.dueDate || null,
                assignedUserId: currentTask.assignedUserId || 1 
            })
        });

        if (response.ok) {
            showToast("Görev durumu güncellendi.", "success");
            fetchTasks(); 
        } else {
            const errorText = await response.text();
            showToast(`Güncelleme yapılamadı: ${errorText}`, "error");
            fetchTasks();
        }
    } catch (error) {
        console.error('Durum güncellenirken hata:', error);
        showToast('Durum güncellenirken sunucu hatası oluştu.', 'error');
    }
}

// 10. Kullanıcıları Çekme
async function fetchUsers() {
    try {
        const response = await fetch(`${API_BASE_URL}/Users`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const users = await response.json();
            const userSelect = document.getElementById('taskAssignedUserId');
            userSelect.innerHTML = '<option value="" disabled selected>Atanacak Kişiyi Seçin</option>';

            users.forEach(user => {
                const option = document.createElement('option');
                const userId = user.id || user.Id;
                const userName = user.username || user.Username || user.email || user.Email || `Kullanıcı ${userId}`;

                option.value = userId; 
                option.textContent = userName; 
                userSelect.appendChild(option);
            });
        }
    } catch (error) {
        console.error('Kullanıcılar çekilemedi:', error);
    }
}

// 11.1 Düzenleme Modalını Aç ve Verileri Doldur
function openEditModal(taskId) {
    const task = allTasks.find(t => (t.id || t.Id) == taskId);
    if (!task) return;

    document.getElementById('editTaskId').value = taskId;
    document.getElementById('editTaskTitle').value = task.title || '';
    document.getElementById('editTaskDescription').value = task.description || '';
    
    // YENİ EKLENEN SATIR: Kategori bilgisini modaldaki seçiciye (select) atıyoruz
    document.getElementById('editTaskCategory').value = task.category || 2;
    
    if (task.dueDate) {
        const dateOnly = task.dueDate.split('T')[0];
        document.getElementById('editTaskDueDate').value = dateOnly;
    } else {
        document.getElementById('editTaskDueDate').value = '';
    }

    document.getElementById('editModal').style.display = 'flex';
}

// 11.2 Modalı Kapat
function closeEditModal() {
    document.getElementById('editModal').style.display = 'none';
}

// 11.3 Düzenlenen Verileri API'ye Gönder (PUT)
async function saveTaskEdit() {
    const taskId = document.getElementById('editTaskId').value;
    const title = document.getElementById('editTaskTitle').value;
    const description = document.getElementById('editTaskDescription').value;
    const dueDate = document.getElementById('editTaskDueDate').value;
    
    // YENİ EKLENEN: Modal'daki kategori seçimini okuyoruz
    const category = document.getElementById('editTaskCategory').value;

    if (!title) {
        showToast('Görev başlığı boş bırakılamaz!', 'warning');
        return;
    }

    const currentTask = allTasks.find(t => (t.id || t.Id) == taskId);
    if (!currentTask) return;

    try {
        const response = await fetch(`${API_BASE_URL}/Tasks/${taskId}`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ 
                id: parseInt(taskId), 
                title: title, 
                description: description, 
                status: currentTask.status, 
                dueDate: dueDate ? new Date(dueDate).toISOString() : null,
                assignedUserId: currentTask.assignedUserId || 1,
                // YENİ EKLENEN: Kategoriyi API'ye gönderiyoruz
                category: parseInt(category)
            })
        });

        if (response.ok) {
            showToast("Görev başarıyla güncellendi.", "success");
            closeEditModal();
            fetchTasks();
        } else {
            const errorText = await response.text();
            showToast(`Güncellenemedi: ${errorText}`, 'error');
        }
    } catch (error) {
        console.error('Düzenleme sırasında hata:', error);
        showToast('Sunucu bağlantı hatası.', 'error');
    }
}

// 12. Modern Toast Bildirim Fonksiyonu
function showToast(message, type = 'success') {
    const container = document.getElementById('toast-container');
    if (!container) return; // HTML'de container yoksa çalışmasın

    const toast = document.createElement('div');
    
    // Tip'e göre renk ayarları
    const bgColor = type === 'success' ? '#10b981' : (type === 'error' ? '#ef4444' : '#f59e0b');
    const color = 'white';

    toast.style.cssText = `
        background-color: ${bgColor};
        color: ${color};
        padding: 12px 20px;
        border-radius: 8px;
        box-shadow: 0 4px 10px rgba(0,0,0,0.15);
        font-size: 14px;
        font-weight: 500;
        opacity: 0;
        transform: translateX(100%);
        transition: all 0.3s cubic-bezier(0.68, -0.55, 0.265, 1.55);
    `;
    toast.textContent = message;

    container.appendChild(toast);

    // Fade-in ve Slide-in animasyonu (Küçük bir gecikme ile)
    setTimeout(() => { 
        toast.style.opacity = '1'; 
        toast.style.transform = 'translateX(0)';
    }, 10);

    // 3 saniye sonra silinme efekti
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(100%)';
        setTimeout(() => { toast.remove(); }, 300);
    }, 3000);
}

//13. Sayfalama (Pagination) Çizim Fonksiyonu
function renderPagination() {
    const paginationDiv = document.getElementById('pagination-controls');
    if (!paginationDiv) return;
    
    paginationDiv.innerHTML = '';
    
    if (totalPages <= 1) return; // Tek sayfa varsa numaraları gizle
    
    // Önceki Butonu
    const prevBtn = document.createElement('button');
    prevBtn.innerHTML = '<i class="fa-solid fa-chevron-left"></i>';
    prevBtn.className = 'action-btn';
    prevBtn.style.cssText = `background: ${currentPage === 1 ? '#e5e7eb' : '#3b82f6'}; color: ${currentPage === 1 ? '#9ca3af' : 'white'}; padding: 8px 12px;`;
    prevBtn.disabled = currentPage === 1;
    prevBtn.onclick = () => { if (currentPage > 1) { currentPage--; fetchTasks(); } };
    paginationDiv.appendChild(prevBtn);
    
    // Sayfa Numaraları
    for (let i = 1; i <= totalPages; i++) {
        const pageBtn = document.createElement('button');
        pageBtn.textContent = i;
        pageBtn.className = 'action-btn';
        pageBtn.style.cssText = `background: ${currentPage === i ? '#10b981' : '#e5e7eb'}; color: ${currentPage === i ? 'white' : '#374151'}; padding: 8px 12px; font-weight: bold;`;
        pageBtn.onclick = () => { currentPage = i; fetchTasks(); };
        paginationDiv.appendChild(pageBtn);
    }
    
    // Sonraki Butonu
    const nextBtn = document.createElement('button');
    nextBtn.innerHTML = '<i class="fa-solid fa-chevron-right"></i>';
    nextBtn.className = 'action-btn';
    nextBtn.style.cssText = `background: ${currentPage === totalPages ? '#e5e7eb' : '#3b82f6'}; color: ${currentPage === totalPages ? '#9ca3af' : 'white'}; padding: 8px 12px;`;
    nextBtn.disabled = currentPage === totalPages;
    nextBtn.onclick = () => { if (currentPage < totalPages) { currentPage++; fetchTasks(); } };
    paginationDiv.appendChild(nextBtn);
}

// 14. Kullanıcı Moadlı Fonksiyonları
function openUsersModal() {
    document.getElementById('usersModal').style.display = 'flex';
    loadUsersList();
}

function closeUsersModal() {
    document.getElementById('usersModal').style.display = 'none';
}

async function loadUsersList() {
    const tbody = document.getElementById('usersTableBody');
    tbody.innerHTML = `<tr><td colspan="4" style="text-align: center;"><i class="fa-solid fa-spinner fa-spin"></i> Yükleniyor...</td></tr>`;

    try {
        const response = await fetch(`${API_BASE_URL}/Users`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const users = await response.json();
            tbody.innerHTML = '';
            
            users.forEach(u => {
                const tr = document.createElement('tr');
                const userId = u.id || u.Id;
                const roleValue = u.role || u.Role;
                
                tr.innerHTML = `
                    <td>${u.firstName || u.FirstName} ${u.lastName || u.LastName}</td>
                    <td>${u.email || u.Email}</td>
                    <td>
                        <select id="userRoleSelect_${userId}" class="status-select" style="background: #e5e7eb;">
                            <option value="1" ${roleValue === 1 ? 'selected' : ''}>Admin</option>
                            <option value="2" ${roleValue === 2 ? 'selected' : ''}>Personel</option>
                        </select>
                    </td>
                    <td>
                        <button onclick="saveUserRole(${userId})" style="padding: 6px 12px; background: #10b981; color: white; border: none; border-radius: 4px; cursor: pointer;">Kaydet</button>
                    </td>
                `;
                tbody.appendChild(tr);
            });
        }
    } catch (error) {
        console.error('Kullanıcılar yüklenirken hata:', error);
        tbody.innerHTML = `<tr><td colspan="4" style="text-align: center; color: red;">Veriler çekilemedi!</td></tr>`;
    }
}

async function saveUserRole(userId) {
    const newRole = document.getElementById(`userRoleSelect_${userId}`).value;

    try {
        const response = await fetch(`${API_BASE_URL}/Users/${userId}/role`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ 
                userId: parseInt(userId), 
                newRole: parseInt(newRole) 
            })
        });

        if (response.ok) {
            showToast("Kullanıcı rolü başarıyla güncellendi.", "success");
        } else {
            const errorText = await response.text();
            showToast(`Hata: ${errorText}`, "error");
        }
    } catch (error) {
        console.error('Rol güncellenirken hata:', error);
        showToast('Sunucu bağlantı hatası.', 'error');
    }
}

// --- SIRALAMA (SORTING) FONKSİYONLARI ---
function handleSort(column) {
    // Eğer aynı kolona tıklandıysa yönü değiştir, farklı kolonsa o kolonu seç ve A-Z yap
    if (currentSortBy === column) {
        isSortDescending = !isSortDescending; 
    } else {
        currentSortBy = column;
        isSortDescending = false; // Yeni kolon seçildiğinde genelde Artan (A-Z) başlanır
    }
        
    updateSortIcons();
    currentPage = 1; // Sıralama değişince kafa karışıklığı olmaması için 1. sayfaya dönüyoruz
    fetchTasks();
}

function updateSortIcons() {
    // 1. Bütün ok ikonlarını varsayılan (çift yönlü gri ok) haline getir
    document.querySelectorAll('.sort-icon').forEach(icon => {
        icon.className = 'fa-solid fa-sort sort-icon';
    });
        
    // 2. Sadece aktif olan kolondaki okun yönünü ve rengini değiştir
    const activeIcon = document.getElementById(`icon-${currentSortBy}`);
    if (activeIcon) {
        activeIcon.className = isSortDescending 
            ? 'fa-solid fa-arrow-down-z-a sort-icon active' 
            : 'fa-solid fa-arrow-down-a-z sort-icon active';
    }
}

// --- GÖREV GEÇMİŞİ (AUDIT LOG) İŞLEMLERİ ---
async function openHistoryModal(taskId) {
    document.getElementById('historyModal').style.display = 'flex';
    const list = document.getElementById('historyList');
    list.innerHTML = '<li style="text-align: center; color: #6b7280;">Yükleniyor...</li>';

    try {
        const response = await fetch(`${API_BASE_URL}/Tasks/${taskId}/history`, {
            method: 'GET',
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (response.ok) {
            const history = await response.json();
            list.innerHTML = '';

            if (history.length === 0) {
                list.innerHTML = '<li style="color: #6b7280; font-style: italic;">Bu göreve ait geçmiş kaydı bulunamadı.</li>';
                return;
            }

            history.forEach(h => {
                const dateObj = new Date(h.createdDate || h.CreatedDate);
                const dateStr = dateObj.toLocaleDateString('tr-TR') + ' ' + dateObj.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
                
                list.innerHTML += `
                    <li style="padding: 10px 0; border-bottom: 1px solid #e5e7eb;">
                        <span style="display: block; font-size: 12px; color: #9ca3af; margin-bottom: 4px;">${dateStr}</span>
                        <span style="color: #374151; font-weight: 500;">${h.actionMessage || h.ActionMessage}</span>
                    </li>
                `;
            });
        }
    } catch (error) {
        console.error("Geçmiş çekilemedi:", error);
        list.innerHTML = '<li style="color: #ef4444;">Veriler çekilirken bir hata oluştu.</li>';
    }
}

function closeHistoryModal() {
    document.getElementById('historyModal').style.display = 'none';
}

// --- YORUM (COMMENT) İŞLEMLERİ ---
async function openCommentModal(taskId) {
    document.getElementById('commentTaskId').value = taskId;
    document.getElementById('commentModal').style.display = 'flex';
    document.getElementById('newCommentText').value = '';
    await loadComments(taskId);
}

function closeCommentModal() {
    document.getElementById('commentModal').style.display = 'none';
}

async function loadComments(taskId) {
    const list = document.getElementById('commentList');
    list.innerHTML = '<p style="text-align:center; color:#6b7280;">Yükleniyor...</p>';

    try {
        const response = await fetch(`${API_BASE_URL}/Tasks/${taskId}/comments`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (response.ok) {
            const comments = await response.json();
            list.innerHTML = '';
            
            if (comments.length === 0) {
                list.innerHTML = '<p style="text-align:center; color:#9ca3af; font-size: 13px;">Henüz not eklenmemiş.</p>';
            }
            
            comments.forEach(c => {
                const date = new Date(c.createdDate || c.CreatedDate).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
                list.innerHTML += `
                    <div style="margin-bottom: 10px; background: white; padding: 10px; border-radius: 6px; border: 1px solid #e5e7eb;">
                        <div style="font-size: 11px; color: #6b7280; margin-bottom: 4px; display: flex; justify-content: space-between;">
                            <strong>${c.userName || c.UserName}</strong> <span>${date}</span>
                        </div>
                        <div style="font-size: 13px; color: #1f2937;">${c.text || c.Text}</div>
                    </div>
                `;
            });
            list.scrollTop = list.scrollHeight; // En alta kaydır
        }
    } catch (err) {
        list.innerHTML = '<p style="color:red;">Hata oluştu.</p>';
    }
}

async function postComment() {
    const taskId = document.getElementById('commentTaskId').value;
    const text = document.getElementById('newCommentText').value;

    if (!text.trim()) return;

    try {
        const response = await fetch(`${API_BASE_URL}/Tasks/${taskId}/comments`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ text: text })
        });

        if (response.ok) {
            document.getElementById('newCommentText').value = '';
            await loadComments(taskId); // Listeyi yenile
        }
    } catch (err) {
        console.error("Yorum gönderilemedi:", err);
    }
}