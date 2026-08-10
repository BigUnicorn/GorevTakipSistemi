import { parseJwt, logout } from './utils.js';
import { fetchWithAuth } from './api.js';

const token = localStorage.getItem('token');
if (!token) logout();

const tokenData = parseJwt(token);
const userRole = tokenData ? (tokenData['role'] || tokenData['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 'User') : 'User';

document.addEventListener('DOMContentLoaded', () => {
    if (userRole === 'Admin') {
        const adminFilterDiv = document.getElementById('adminFilterDiv');
        if (adminFilterDiv) adminFilterDiv.style.display = 'block';
        fetchUsersForFilter();
    }
    fetchStatistics();
});

// HTML'den çağrılan fonksiyonları window nesnesine ekliyoruz
window.logout = logout;
window.applyFilters = applyFilters;

function applyFilters() {
    const userSelect = document.getElementById('userFilterSelect');
    const categorySelect = document.getElementById('categoryFilterSelect');
    
    const userId = userSelect ? userSelect.value : '';
    const categoryId = categorySelect ? categorySelect.value : '';
    
    fetchStatistics(userId, categoryId);
}

async function fetchStatistics(selectedUserId = '', selectedCategoryId = '') {
    try {
        const params = new URLSearchParams();
        if (selectedUserId) params.append('userId', selectedUserId);
        if (selectedCategoryId) params.append('categoryId', selectedCategoryId);
        
        const response = await fetchWithAuth(`/Tasks/statistics?${params.toString()}`, { method: 'GET' });
        
        if (response && response.ok) {
            const stats = await response.json();
            
            document.getElementById('valTotal').textContent = stats.totalTasks;
            document.getElementById('valTodo').textContent = stats.todoTasks;
            document.getElementById('valInProgress').textContent = stats.inProgressTasks;
            document.getElementById('valCompleted').textContent = stats.completedTasks;
            
            renderChart(stats);
            renderCategoryChart(stats);
        }
    } catch (error) {
        console.error('İstatistikler çekilirken hata oluştu:', error);
    }
}

async function fetchUsersForFilter() {
    try {
        const response = await fetchWithAuth('/Users', { method: 'GET' });
        if (response && response.ok) {
            const users = await response.json();
            const select = document.getElementById('userFilterSelect');
            
            users.forEach(user => {
                const option = document.createElement('option');
                option.value = user.id || user.Id;
                option.textContent = `${user.firstName || user.FirstName} ${user.lastName || user.LastName} (${user.email || user.Email})`;
                select.appendChild(option);
            });
        }
    } catch (error) {
        console.error('Kullanıcı filtre listesi çekilemedi:', error);
    }
}

// Chart.js Tanımlamaları (Mevcut kodunla aynı)
let myChart = null;
function renderChart(stats) {
    const ctx = document.getElementById('taskChart').getContext('2d');
    if (myChart != null) myChart.destroy();
    
    myChart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Yapılacaklar', 'Devam Edenler', 'Tamamlananlar'],
            datasets: [{
                data: [stats.todoTasks, stats.inProgressTasks, stats.completedTasks],
                backgroundColor: ['#6c757d', '#f59e0b', '#10b981'],
                borderWidth: 0,
                hoverOffset: 10
            }]
        },
        options: { responsive: true, cutout: '65%' }
    });
}

let categoryChartInstance = null;
function renderCategoryChart(stats) {
    const ctx = document.getElementById('categoryChart').getContext('2d');
    if (categoryChartInstance != null) categoryChartInstance.destroy();
    
    categoryChartInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: ['Frontend', 'Backend', 'Veritabanı', 'BugFix', 'Mobil', 'DevOps'],
            datasets: [{
                label: 'Görev Sayısı',
                data: [stats.frontendTasks, stats.backendTasks, stats.databaseTasks, stats.bugFixTasks, stats.mobileTasks, stats.devOpsTasks],
                backgroundColor: ['#0284c7', '#7c3aed', '#db2777', '#dc2626', '#ea580c', '#16a34a'],
                borderRadius: 6, borderWidth: 0
            }]
        },
        options: { responsive: true, plugins: { legend: { display: false } } }
    });
}