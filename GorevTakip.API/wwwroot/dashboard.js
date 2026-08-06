const API_BASE_URL = '/api';
const token = localStorage.getItem('token');

if (!token) {
    window.location.href = 'index.html';
}

document.addEventListener('DOMContentLoaded', () => {
    fetchStatistics();
});

async function fetchStatistics() {
    try {
        const response = await fetch(`${API_BASE_URL}/Tasks/statistics`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const stats = await response.json();
            
            // Üstteki kartları doldur
            document.getElementById('valTotal').textContent = stats.totalTasks;
            document.getElementById('valTodo').textContent = stats.todoTasks;
            document.getElementById('valInProgress').textContent = stats.inProgressTasks;
            document.getElementById('valCompleted').textContent = stats.completedTasks;

            // Grafiği Çiz
            renderChart(stats);
        } else if (response.status === 401) {
            alert('Oturumunuz süresi dolmuş.');
            logout();
        }
    } catch (error) {
        console.error('İstatistikler çekilirken hata oluştu:', error);
    }
}

let myChart = null; // Eski grafiği hafızada tutmak için

function renderChart(stats) {
    const ctx = document.getElementById('taskChart').getContext('2d');

    // Eğer sayfada zaten çizilmiş bir grafik varsa onu yok et (yenilenirse üst üste binmesin)
    if (myChart != null) {
        myChart.destroy();
    }

    // Chart.js konfigürasyonu (Doughnut - Halka Grafik)
    myChart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Yapılacaklar', 'Devam Edenler', 'Tamamlananlar'],
            datasets: [{
                data: [stats.todoTasks, stats.inProgressTasks, stats.completedTasks],
                backgroundColor: [
                    '#6c757d', // Gri (Todo)
                    '#f59e0b', // Sarı/Turuncu (InProgress)
                    '#10b981'  // Yeşil (Done)
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
            cutout: '65%' // Ortadaki boşluğun büyüklüğü
        }
    });
}

function logout() {
    localStorage.removeItem('token');
    window.location.href = 'index.html';
}