// js/tasks.js
import { showToast, parseJwt, formatDate, logout, escapeHtml } from './utils.js';
import { fetchWithAuth } from './api.js';


const token = localStorage.getItem('token');

if (!token) {
    logout();
}

let allTasks = [];          
let currentFilter = 'all';  
let currentSearchQuery = ''; 
let taskToDeleteId = null;  
let currentPage = 1;
const pageSize = 5; 
let totalPages = 1;
let currentSortBy = 'duedate'; 
let isSortDescending = true;   

let userRole = '';
const tokenData = token ? parseJwt(token) : null;
if (tokenData) {
    userRole = tokenData['role'] || tokenData['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 'User';
}

// HTML'den tetiklenen (inline) fonksiyonları global window nesnesine bağlıyoruz
Object.assign(window, {
    logout,
    filterTasks,
    handleSearch,
    createTask,
    openDeleteModal,
    closeDeleteModal,
    confirmDeleteAction,
    updateTaskStatus,
    openEditModal,
    closeEditModal,
    saveTaskEdit,
    openUsersModal,
    closeUsersModal,
    saveUserRole,
    handleSort,
    openHistoryModal,
    closeHistoryModal,
    openCommentModal,
    closeCommentModal,
    postComment
});

document.addEventListener('DOMContentLoaded', () => {
    fetchTasks();
    fetchUsers();

    const confirmBtn = document.getElementById('confirmDeleteBtn');
    if(confirmBtn) {
        confirmBtn.addEventListener('click', confirmDeleteAction);
    }

    if (userRole === 'Admin') {
        const adminBtn = document.getElementById('adminUsersBtn');
        if (adminBtn) {
            adminBtn.style.display = 'inline-block'; 
        }
    } else {
        // Admin değilse form alanını gizle ve listeyi genişlet
        const formSection = document.querySelector('.form-section');
        const listSection = document.querySelector('.list-section');
        if (formSection) formSection.style.display = 'none';
        if (listSection) listSection.style.flex = '100%';
    }
});

async function fetchTasks() {
    const tbody = document.getElementById('tasksTableBody');
    tbody.innerHTML = `<tr><td colspan="7" style="text-align: center; color: #6b7280;"><i class="fa-solid fa-spinner fa-spin"></i> Yükleniyor...</td></tr>`;
    
    const params = new URLSearchParams({
        PageNumber: currentPage,
        PageSize: pageSize
    });

    if (currentFilter !== 'all') params.append('Status', currentFilter);
    if (currentSearchQuery) params.append('SearchText', currentSearchQuery);
    if (currentSortBy) {
        params.append('SortBy', currentSortBy);
        params.append('SortDescending', isSortDescending);
    }

    try {
        const response = await fetchWithAuth(`/Tasks?${params.toString()}`, { method: 'GET' });

        if (response && response.ok) {
            const result = await response.json();
            
            allTasks = result.data || result.Data || []; 
            totalPages = result.totalPages || result.TotalPages || 1;
            const totalRecords = result.totalRecords || result.TotalRecords || 0;
            
            renderTasks(allTasks);
            renderPagination();
            
            const statTotal = document.getElementById('statTotal');
            if (statTotal) statTotal.textContent = `Bulunan Görev: ${totalRecords}`;

        } else if (response) {
            const errText = await response.text();
            console.error("API Hatası:", errText);
            tbody.innerHTML = `<tr><td colspan="7" style="text-align: center; color: #ef4444; font-weight: bold;">Sunucu Hatası: Hata detayını görmek için F12 Konsola bakın.</td></tr>`;
            showToast("Tüm görevler çekilirken sunucuda bir hata oluştu.", "error");
        }
    } catch (error) {
        console.error('Görevler çekilirken hata oluştu:', error);
        tbody.innerHTML = `<tr><td colspan="7" style="text-align: center; color: #ef4444;">Görevler yüklenirken sunucuya ulaşılamadı!</td></tr>`;
    }
}

function filterTasks(status) {
    currentFilter = status;
    currentPage = 1; 
    fetchTasks();
}

let searchTimeout;
function handleSearch(query) {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => {
        currentSearchQuery = query.toLowerCase().trim();
        currentPage = 1; 
        fetchTasks();
    }, 400); 
}

function updateStats() {
    const total = allTasks.length;
    const completed = allTasks.filter(t => t.status === 3).length;
    
    const statTotal = document.getElementById('statTotal');
    const statCompleted = document.getElementById('statCompleted');
    
    if (statTotal) statTotal.textContent = `Toplam: ${total}`;
    if (statCompleted) statCompleted.textContent = `Tamamlanan: ${completed}`;
}

function renderTasks(tasks) {
    const tbody = document.getElementById('tasksTableBody');
    tbody.innerHTML = '';

    if (tasks.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align: center; color: #6b7280; padding: 20px;">Görev bulunamadı.</td></tr>`;
        return;
    }

    tasks.forEach(task => {
        const tr = document.createElement('tr');
        const taskId = task.id || task.Id;

        let statusClass = "background-color: #e5e7eb; color: #374151;"; 
        if (task.status === 2) statusClass = "background-color: #fef08a; color: #854d0e;"; 
        if (task.status === 3) statusClass = "background-color: #bbf7d0; color: #166534;"; 

        let rowStyle = task.status === 3 ? "opacity: 0.7;" : "";
        tr.style = rowStyle;

        let actionButtons = '';
        
        const historyBtn = `
            <button onclick="openHistoryModal(${taskId})" class="action-btn" style="background-color: #6366f1; color: white;" title="Geçmişi Gör">
                <i class="fa-solid fa-clock-rotate-left"></i>
            </button>
        `;
        
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

        let categoryLabel = "Belirsiz";
        let catBg = "#f3f4f6", catColor = "#374151";

        switch(task.category || task.Category) {
            case 1: categoryLabel = "Frontend"; catBg = "#e0f2fe"; catColor = "#0284c7"; break;
            case 2: categoryLabel = "Backend"; catBg = "#ede9fe"; catColor = "#7c3aed"; break;
            case 3: categoryLabel = "Veritabanı"; catBg = "#fce7f3"; catColor = "#db2777"; break;
            case 4: categoryLabel = "BugFix"; catBg = "#fee2e2"; catColor = "#dc2626"; break;
            case 5: categoryLabel = "Mobil"; catBg = "#ffedd5"; catColor = "#ea580c"; break;
            case 6: categoryLabel = "DevOps"; catBg = "#dcfce7"; catColor = "#16a34a"; break;
        }

        const categoryBadge = `<span style="background-color: ${catBg}; color: ${catColor}; padding: 4px 8px; border-radius: 6px; font-size: 12px; font-weight: 600;">${categoryLabel}</span>`;

        let isOverdue = false;
        if (task.dueDate && task.status !== 3) { 
            const today = new Date();
            const due = new Date(task.dueDate);
            today.setHours(0, 0, 0, 0);
            due.setHours(0, 0, 0, 0);
            if (due < today) isOverdue = true;
        }

        const dateIcon = isOverdue 
            ? '<i class="fa-solid fa-triangle-exclamation" style="color: #ef4444; margin-right:5px;" title="Gecikmiş Görev!"></i>' 
            : '<i class="fa-regular fa-calendar" style="margin-right:5px; color:#9ca3af;"></i>';

        const dateStyle = isOverdue 
            ? 'color: #ef4444; font-weight: bold; background: #fee2e2; padding: 4px 8px; border-radius: 4px;' 
            : '';

        tr.innerHTML = `
            <td><strong>${escapeHtml(task.title)}</strong></td>
            <td style="color: #6b7280;">${escapeHtml(task.description)}</td>
            <td>${categoryBadge}</td>
            <td><span style="${dateStyle}">${dateIcon}${formatDate(task.dueDate)}</span></td>
            <td>
                <div style="display: flex; align-items: center; gap: 8px;">
                    <div style="width: 28px; height: 28px; border-radius: 50%; background-color: #3b82f6; color: white; display: flex; justify-content: center; align-items: center; font-size: 12px; font-weight: bold;">
                        ${escapeHtml(task.assignedUserName || 'B').charAt(0).toUpperCase()}
                    </div>
                    <span>${escapeHtml(task.assignedUserName || 'Bilinmiyor')}</span>
                </div>
            </td>
            <td>
                <select onchange="updateTaskStatus(${taskId}, this.value)" class="status-select" style="${statusClass}">
                    <option value="1" ${task.status === 1 ? 'selected' : ''}>Yapılacak</option>
                    <option value="2" ${task.status === 2 ? 'selected' : ''}>Devam Ediyor</option>
                    <option value="3" ${task.status === 3 ? 'selected' : ''}>Tamamlandı</option>
                </select>
            </td>
            <td>${actionButtons}</td>
        `;
        tbody.appendChild(tr);
    });
}

async function createTask() {
    const title = document.getElementById('taskTitle').value;
    const description = document.getElementById('taskDescription').value;
    const assignedUserId = document.getElementById('taskAssignedUserId').value;
    const dueDate = document.getElementById('taskDueDate').value;
    const category = document.getElementById('taskCategory').value;
    
    const submitBtn = document.querySelector('.btn-success');
    const originalBtnText = submitBtn ? submitBtn.innerText : 'Ekle';

    if (!title || !assignedUserId || !category) {
        showToast("Lütfen başlık, atanacak kullanıcı ve kategoriyi seçin.", "error");
        return;
    }

    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerText = "Ekleniyor...";
    }

    try {
        const response = await fetchWithAuth('/Tasks', {
            method: 'POST',
            body: JSON.stringify({ 
                title: title, 
                description: description,
                assignedUserId: parseInt(assignedUserId),
                category: parseInt(category), 
                dueDate: dueDate ? new Date(dueDate).toISOString() : null 
            })
        });

        if (response && response.ok) {
            showToast("Görev başarıyla eklendi!", "success");
            
            document.getElementById('taskTitle').value = '';
            document.getElementById('taskDescription').value = '';
            document.getElementById('taskAssignedUserId').value = '';
            document.getElementById('taskDueDate').value = '';
            document.getElementById('taskCategory').value = '';
            
            fetchTasks();
        } else if (response) {
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
        const response = await fetchWithAuth(`/Tasks/${taskId}`, { method: 'DELETE' });
        
        if (response && response.ok) {
            showToast("Görev başarıyla silindi.", "success");
            fetchTasks(); 
        } else if (response) {
            showToast('Görev silinirken hata oluştu.', 'error');
        }
    } catch (error) {
        console.error('Silme hatası:', error);
        showToast('Sunucu bağlantı hatası.', 'error');
    }
}

async function updateTaskStatus(taskId, newStatus) {
    const currentTask = allTasks.find(t => (t.id || t.Id) == taskId);
    if (!currentTask) return;

    try {
        const response = await fetchWithAuth(`/Tasks/${taskId}`, {
            method: 'PUT',
            body: JSON.stringify({ 
                id: parseInt(taskId), 
                title: currentTask.title || currentTask.Title, 
                description: currentTask.description || "", 
                status: parseInt(newStatus), 
                dueDate: currentTask.dueDate || null,
                assignedUserId: currentTask.assignedUserId || 1,
                category: currentTask.category || currentTask.Category
            })
        });

        if (response && response.ok) {
            showToast("Görev durumu güncellendi.", "success");
            fetchTasks(); 
        } else if (response) {
            const errorText = await response.text();
            showToast(`Güncelleme yapılamadı: ${errorText}`, "error");
            fetchTasks();
        }
    } catch (error) {
        console.error('Durum güncellenirken hata:', error);
        showToast('Durum güncellenirken sunucu hatası oluştu.', 'error');
    }
}

async function fetchUsers() {
    try {
        const response = await fetchWithAuth('/Users', { method: 'GET' });

        if (response && response.ok) {
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

function openEditModal(taskId) {
    const task = allTasks.find(t => (t.id || t.Id) == taskId);
    if (!task) return;

    document.getElementById('editTaskId').value = taskId;
    document.getElementById('editTaskTitle').value = task.title || '';
    document.getElementById('editTaskDescription').value = task.description || '';
    document.getElementById('editTaskCategory').value = task.category || 2;
    
    if (task.dueDate) {
        const dateOnly = task.dueDate.split('T')[0];
        document.getElementById('editTaskDueDate').value = dateOnly;
    } else {
        document.getElementById('editTaskDueDate').value = '';
    }

    document.getElementById('editModal').style.display = 'flex';
}

function closeEditModal() {
    document.getElementById('editModal').style.display = 'none';
}

async function saveTaskEdit() {
    const taskId = document.getElementById('editTaskId').value;
    const title = document.getElementById('editTaskTitle').value;
    const description = document.getElementById('editTaskDescription').value;
    const dueDate = document.getElementById('editTaskDueDate').value;
    const category = document.getElementById('editTaskCategory').value;

    if (!title) {
        showToast('Görev başlığı boş bırakılamaz!', 'warning');
        return;
    }

    const currentTask = allTasks.find(t => (t.id || t.Id) == taskId);
    if (!currentTask) return;

    try {
        const response = await fetchWithAuth(`/Tasks/${taskId}`, {
            method: 'PUT',
            body: JSON.stringify({ 
                id: parseInt(taskId), 
                title: title, 
                description: description, 
                status: currentTask.status, 
                dueDate: dueDate ? new Date(dueDate).toISOString() : null,
                assignedUserId: currentTask.assignedUserId || 1,
                category: parseInt(category)
            })
        });

        if (response && response.ok) {
            showToast("Görev başarıyla güncellendi.", "success");
            closeEditModal();
            fetchTasks();
        } else if (response) {
            const errorText = await response.text();
            showToast(`Güncellenemedi: ${errorText}`, 'error');
        }
    } catch (error) {
        console.error('Düzenleme sırasında hata:', error);
        showToast('Sunucu bağlantı hatası.', 'error');
    }
}

function renderPagination() {
    const paginationDiv = document.getElementById('pagination-controls');
    if (!paginationDiv) return;
    
    paginationDiv.innerHTML = '';
    
    if (totalPages <= 1) return; 
    
    const prevBtn = document.createElement('button');
    prevBtn.innerHTML = '<i class="fa-solid fa-chevron-left"></i>';
    prevBtn.className = 'action-btn';
    prevBtn.style.cssText = `background: ${currentPage === 1 ? '#e5e7eb' : '#3b82f6'}; color: ${currentPage === 1 ? '#9ca3af' : 'white'}; padding: 8px 12px;`;
    prevBtn.disabled = currentPage === 1;
    prevBtn.onclick = () => { if (currentPage > 1) { currentPage--; fetchTasks(); } };
    paginationDiv.appendChild(prevBtn);
    
    for (let i = 1; i <= totalPages; i++) {
        const pageBtn = document.createElement('button');
        pageBtn.textContent = i;
        pageBtn.className = 'action-btn';
        pageBtn.style.cssText = `background: ${currentPage === i ? '#10b981' : '#e5e7eb'}; color: ${currentPage === i ? 'white' : '#374151'}; padding: 8px 12px; font-weight: bold;`;
        pageBtn.onclick = () => { currentPage = i; fetchTasks(); };
        paginationDiv.appendChild(pageBtn);
    }
    
    const nextBtn = document.createElement('button');
    nextBtn.innerHTML = '<i class="fa-solid fa-chevron-right"></i>';
    nextBtn.className = 'action-btn';
    nextBtn.style.cssText = `background: ${currentPage === totalPages ? '#e5e7eb' : '#3b82f6'}; color: ${currentPage === totalPages ? '#9ca3af' : 'white'}; padding: 8px 12px;`;
    nextBtn.disabled = currentPage === totalPages;
    nextBtn.onclick = () => { if (currentPage < totalPages) { currentPage++; fetchTasks(); } };
    paginationDiv.appendChild(nextBtn);
}

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
        const response = await fetchWithAuth('/Users', { method: 'GET' });

        if (response && response.ok) {
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
        const response = await fetchWithAuth(`/Users/${userId}/role`, {
            method: 'PUT',
            body: JSON.stringify({ 
                userId: parseInt(userId), 
                newRole: parseInt(newRole) 
            })
        });

        if (response && response.ok) {
            showToast("Kullanıcı rolü başarıyla güncellendi.", "success");
        } else if (response) {
            const errorText = await response.text();
            showToast(`Hata: ${errorText}`, "error");
        }
    } catch (error) {
        console.error('Rol güncellenirken hata:', error);
        showToast('Sunucu bağlantı hatası.', 'error');
    }
}

function handleSort(column) {
    if (currentSortBy === column) {
        isSortDescending = !isSortDescending; 
    } else {
        currentSortBy = column;
        isSortDescending = false; 
    }
        
    updateSortIcons();
    currentPage = 1; 
    fetchTasks();
}

function updateSortIcons() {
    document.querySelectorAll('.sort-icon').forEach(icon => {
        icon.className = 'fa-solid fa-sort sort-icon';
    });
        
    const activeIcon = document.getElementById(`icon-${currentSortBy}`);
    if (activeIcon) {
        activeIcon.className = isSortDescending 
            ? 'fa-solid fa-arrow-down-z-a sort-icon active' 
            : 'fa-solid fa-arrow-down-a-z sort-icon active';
    }
}

async function openHistoryModal(taskId) {
    document.getElementById('historyModal').style.display = 'flex';
    const list = document.getElementById('historyList');
    list.innerHTML = '<li style="text-align: center; color: #6b7280;">Yükleniyor...</li>';

    try {
        const response = await fetchWithAuth(`/Tasks/${taskId}/history`, { method: 'GET' });

        if (response && response.ok) {
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
                        <span style="color: #374151; font-weight: 500;">${escapeHtml(h.actionMessage || h.ActionMessage)}</span>
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
        const response = await fetchWithAuth(`/Tasks/${taskId}/comments`, { method: 'GET' });
        if (response && response.ok) {
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
                            <strong>${escapeHtml(c.userName || c.UserName)}</strong> <span>${date}</span>
                        </div>
                        <div style="font-size: 13px; color: #1f2937;">${escapeHtml(c.text || c.Text)}</div>
                    </div>
                `;
            });
            list.scrollTop = list.scrollHeight; 
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
        const response = await fetchWithAuth(`/Tasks/${taskId}/comments`, {
            method: 'POST',
            body: JSON.stringify({ text: text })
        });

        if (response && response.ok) {
            document.getElementById('newCommentText').value = '';
            await loadComments(taskId); 
        }
    } catch (err) {
        console.error("Yorum gönderilemedi:", err);
    }
}

// --- SIGNALR GERÇEK ZAMANLI İLETİŞİM KODLARI ---
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/taskhub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on("ReceiveTaskUpdate", function (message) {
    console.log("Sunucudan güncelleme geldi:", message);
    fetchTasks(); 
});

connection.on("ReceiveNewComment", function (taskId) {
    const commentModal = document.getElementById('commentModal');
    const activeTaskId = document.getElementById('commentTaskId').value;
    
    if (commentModal.style.display === 'flex' && activeTaskId == taskId) {
        loadComments(taskId);
    }
});

async function startSignalR() {
    try {
        await connection.start();
        console.log("SignalR bağlantısı başarıyla kuruldu.");
    } catch (err) {
        console.error("SignalR bağlantı hatası: ", err);
        setTimeout(startSignalR, 5000); 
    }
};

startSignalR();