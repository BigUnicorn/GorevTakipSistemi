const API_BASE_URL = '/api';
const token = localStorage.getItem('token');

if (!token) {
    window.location.href = 'index.html';
}

// Token çözümleme (Role bakmak için)
function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        return JSON.parse(decodeURIComponent(atob(base64).split('').map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join('')));
    } catch (e) {
        return null;
    }
}

const tokenData = parseJwt(token);
const userRole = tokenData ? (tokenData['role'] || tokenData['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 'User') : 'User';

document.addEventListener('DOMContentLoaded', () => {
    // Eğer kullanıcı Admin ise personel filtre panelini aç ve kullanıcıları doldur
    if (userRole === 'Admin') {
        const adminFilterDiv = document.getElementById('adminFilterDiv');
        if (adminFilterDiv) adminFilterDiv.style.display = 'block';
        fetchUsersForFilter();
    }
    
    // İlk açılışta genel istatistikleri çek
    fetchStatistics();
});

// YENİ EKLENDİ: İki filtreyi de (Personel ve Kategori) okuyup API'ye gönderir
function applyFilters() {
    const userSelect = document.getElementById('userFilterSelect');
    const categorySelect = document.getElementById('categoryFilterSelect');
    
    const userId = userSelect ? userSelect.value : '';
    const categoryId = categorySelect ? categorySelect.value : '';
    
    fetchStatistics(userId, categoryId);
}

// GÜNCELLENDİ: Hem userId hem categoryId alıyor ve URL'i URLSearchParams ile dinamik oluşturuyor
async function fetchStatistics(selectedUserId = '', selectedCategoryId = '') {
    try {
        const params = new URLSearchParams();
        if (selectedUserId) params.append('userId', selectedUserId);
        if (selectedCategoryId) params.append('categoryId', selectedCategoryId);

        const url = `${API_BASE_URL}/Tasks/statistics?${params.toString()}`;

        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const stats = await response.json();
            
            // Kartları güncelle
            document.getElementById('valTotal').textContent = stats.totalTasks;
            document.getElementById('valTodo').textContent = stats.todoTasks;
            document.getElementById('valInProgress').textContent = stats.inProgressTasks;
            document.getElementById('valCompleted').textContent = stats.completedTasks;

            // Grafikleri yeniden çiz
            renderChart(stats);
            renderCategoryChart(stats);
        } else if (response.status === 401) {
            alert('Oturumunuz süresi dolmuş.');
            logout();
        }
    } catch (error) {
        console.error('İstatistikler çekilirken hata oluştu:', error);
    }
}

// Admin için personelleri getiren fonksiyon (Aynı Kaldı)
async function fetchUsersForFilter() {
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
            const select = document.getElementById('userFilterSelect');
            
            users.forEach(user => {
                const option = document.createElement('option');
                const userId = user.id || user.Id;
                const firstName = user.firstName || user.FirstName;
                const lastName = user.lastName || user.LastName;
                const email = user.email || user.Email;

                option.value = userId;
                option.textContent = `${firstName} ${lastName} (${email})`;
                select.appendChild(option);
            });
        }
    } catch (error) {
        console.error('Kullanıcı filtre listesi çekilemedi:', error);
    }
}

let myChart = null;

// Durum Grafiği (Aynı Kaldı)
function renderChart(stats) {
    const ctx = document.getElementById('taskChart').getContext('2d');

    if (myChart != null) {
        myChart.destroy();
    }

    myChart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Yapılacaklar', 'Devam Edenler', 'Tamamlananlar'],
            datasets: [{
                data: [stats.todoTasks, stats.inProgressTasks, stats.completedTasks],
                backgroundColor: [
                    '#6c757d', 
                    '#f59e0b', 
                    '#10b981'  
                ],
                borderWidth: 0,
                hoverOffset: 10
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        font: { family: 'Inter', size: 14 }
                    }
                }
            },
            cutout: '65%'
        }
    });
}

let categoryChartInstance = null; 

// Kategori Grafiği (Aynı Kaldı)
function renderCategoryChart(stats) {
    const ctx = document.getElementById('categoryChart').getContext('2d');

    if (categoryChartInstance != null) {
        categoryChartInstance.destroy();
    }

    categoryChartInstance = new Chart(ctx, {
        type: 'bar', 
        data: {
            labels: ['Frontend', 'Backend', 'Veritabanı', 'BugFix', 'Mobil', 'DevOps'],
            datasets: [{
                label: 'Görev Sayısı',
                data: [
                    stats.frontendTasks, 
                    stats.backendTasks, 
                    stats.databaseTasks, 
                    stats.bugFixTasks, 
                    stats.mobileTasks, 
                    stats.devOpsTasks
                ],
                backgroundColor: [
                    '#0284c7', 
                    '#7c3aed', 
                    '#db2777', 
                    '#dc2626', 
                    '#ea580c', 
                    '#16a34a'  
                ],
                borderRadius: 6, 
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    display: false 
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        stepSize: 1 
                    }
                }
            }
        }
    });
}

function logout() {
    localStorage.removeItem('token');
    window.location.href = 'index.html';
}