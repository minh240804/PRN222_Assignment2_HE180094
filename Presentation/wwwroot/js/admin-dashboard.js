// Admin Dashboard with Real-time Updates via SignalR
(function() {
    'use strict';

    let statsChart = null;
    let currentChartType = 'bar';
    let activityLogItems = [];
    const MAX_ACTIVITY_LOG = 20;

    window.initDashboard = async function(config) {
        const { initialStats } = config;

        // Initialize Chart
        initializeChart(initialStats);

        // Setup SignalR connection
        await setupSignalRConnection();

        // Setup chart type toggle buttons
        setupChartTypeToggle();

        console.log('📊 Dashboard initialized with real-time updates');
    };

    function initializeChart(stats) {
        const ctx = document.getElementById('statsChart');
        if (!ctx) return;

        const chartConfig = {
            type: currentChartType,
            data: {
                labels: ['Articles', 'Accounts', 'Categories', 'Comments'],
                datasets: [{
                    label: 'Total Count',
                    data: [
                        stats.totalArticles,
                        stats.totalAccounts,
                        stats.totalCategories,
                        stats.totalComments
                    ],
                    backgroundColor: [
                        'rgba(78, 115, 223, 0.8)',
                        'rgba(28, 200, 138, 0.8)',
                        'rgba(54, 185, 204, 0.8)',
                        'rgba(246, 194, 62, 0.8)'
                    ],
                    borderColor: [
                        'rgb(78, 115, 223)',
                        'rgb(28, 200, 138)',
                        'rgb(54, 185, 204)',
                        'rgb(246, 194, 62)'
                    ],
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: currentChartType === 'pie',
                        position: 'right'
                    },
                    title: {
                        display: true,
                        text: 'System Statistics',
                        font: {
                            size: 16
                        }
                    }
                },
                scales: currentChartType !== 'pie' ? {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    }
                } : {}
            }
        };

        statsChart = new Chart(ctx, chartConfig);
    }

    function setupChartTypeToggle() {
        const buttons = document.querySelectorAll('[data-chart-type]');
        buttons.forEach(button => {
            button.addEventListener('click', function() {
                const newType = this.getAttribute('data-chart-type');
                
                // Update active button
                buttons.forEach(btn => btn.classList.remove('active'));
                this.classList.add('active');

                // Change chart type
                changeChartType(newType);
            });
        });
    }

    function changeChartType(type) {
        if (!statsChart) return;

        currentChartType = type;
        
        // Update chart configuration
        statsChart.config.type = type;
        statsChart.options.plugins.legend.display = (type === 'pie');
        
        if (type === 'pie') {
            delete statsChart.options.scales;
        } else {
            statsChart.options.scales = {
                y: {
                    beginAtZero: true,
                    ticks: {
                        stepSize: 1
                    }
                }
            };
        }

        statsChart.update();
        console.log(`📈 Chart type changed to: ${type}`);
    }

    async function setupSignalRConnection() {
        if (!window.connection) {
            console.error('❌ SignalR connection not available');
            updateConnectionStatus(false);
            return;
        }

        const connection = window.connection;

        try {
            // Wait for connection to be ready
            if (connection.state !== signalR.HubConnectionState.Connected) {
                console.log('⏳ Waiting for SignalR connection to establish...');
                updateConnectionStatus(false);
                
                // Wait up to 10 seconds for connection
                let retries = 0;
                while (connection.state !== signalR.HubConnectionState.Connected && retries < 20) {
                    await new Promise(resolve => setTimeout(resolve, 500));
                    retries++;
                }
                
                if (connection.state !== signalR.HubConnectionState.Connected) {
                    console.error('❌ SignalR connection timeout');
                    updateConnectionStatus(false);
                    return;
                }
            }

            // Join admin dashboard group
            await connection.invoke('JoinDashboardGroup');
            console.log('✅ Joined admin dashboard group');
            updateConnectionStatus(true);

            // Remove old listeners to avoid duplicates
            connection.off('DashboardUpdate');
            connection.off('NewArticlePublished');
            connection.off('ArticleDeleted');
            connection.off('ReceiveNewAccountNotification');
            connection.off('AccountDeactivated');
            connection.off('ReceiveCreateCategoryNotification');
            connection.off('ReceiveComment');
            connection.off('CommentDeleted');

            // Listen for dashboard updates
            connection.on('DashboardUpdate', handleDashboardUpdate);

            // Listen for specific entity events
            connection.on('NewArticlePublished', (author, title) => {
                handleEntityEvent('create', 'article', `New article published: "${title}" by ${author}`);
            });

            connection.on('ArticleDeleted', (id, title) => {
                handleEntityEvent('delete', 'article', `Article deleted: "${title}"`);
            });

            connection.on('ReceiveNewAccountNotification', (message) => {
                handleEntityEvent('create', 'account', message);
            });

            connection.on('AccountDeactivated', (accountId) => {
                handleEntityEvent('update', 'account', `Account ${accountId} deactivated`);
            });

            connection.on('ReceiveCreateCategoryNotification', (message) => {
                handleEntityEvent('create', 'category', message);
            });

            connection.on('ReceiveComment', (data) => {
                handleEntityEvent('create', 'comment', `New comment by ${data.user}`);
            });

            connection.on('CommentDeleted', (commentId) => {
                handleEntityEvent('delete', 'comment', `Comment #${commentId} deleted`);
            });

            // Handle connection state changes
            connection.onreconnecting(() => {
                console.log('🔄 Dashboard reconnecting...');
                updateConnectionStatus(false);
            });

            connection.onreconnected(async () => {
                console.log('✅ Dashboard reconnected');
                updateConnectionStatus(true);
                await connection.invoke('JoinDashboardGroup');
                await refreshStats();
            });

            connection.onclose(() => {
                console.log('❌ Dashboard connection closed');
                updateConnectionStatus(false);
            });

        } catch (error) {
            console.error('❌ Dashboard SignalR setup error:', error);
            updateConnectionStatus(false);
        }
    }

    function handleDashboardUpdate(data) {
        const { eventType, entityType, message, timestamp } = data;
        handleEntityEvent(eventType, entityType, message);
    }

    async function handleEntityEvent(eventType, entityType, message) {
        console.log(`📊 Dashboard event: ${eventType} - ${entityType} - ${message}`);
        
        // Add to activity log
        addActivityLog(eventType, entityType, message);

        // Refresh statistics
        await refreshStats();

        // Animate the affected card
        animateStatCard(entityType);
    }

    function addActivityLog(eventType, entityType, message) {
        const now = new Date();
        const timeStr = now.toLocaleTimeString();
        
        const iconMap = {
            create: 'bi-plus-circle text-success',
            update: 'bi-pencil text-info',
            delete: 'bi-trash text-danger',
            publish: 'bi-check-circle text-success'
        };

        const icon = iconMap[eventType] || 'bi-info-circle';

        const logItem = {
            eventType,
            entityType,
            message,
            timestamp: now,
            html: `
                <div class="list-group-item list-group-item-action animate__animated animate__fadeInDown">
                    <div class="d-flex w-100 justify-content-between">
                        <h6 class="mb-1">
                            <i class="bi ${icon}"></i>
                            ${capitalizeFirst(entityType)} ${capitalizeFirst(eventType)}
                        </h6>
                        <small class="text-muted">${timeStr}</small>
                    </div>
                    <p class="mb-1">${escapeHtml(message)}</p>
                </div>
            `
        };

        activityLogItems.unshift(logItem);
        
        // Keep only last N items
        if (activityLogItems.length > MAX_ACTIVITY_LOG) {
            activityLogItems = activityLogItems.slice(0, MAX_ACTIVITY_LOG);
        }

        updateActivityLogUI();
    }

    function updateActivityLogUI() {
        const logContainer = document.getElementById('activityLog');
        if (!logContainer) return;

        if (activityLogItems.length === 0) {
            logContainer.innerHTML = '<div class="list-group-item text-muted text-center">No recent activity</div>';
        } else {
            logContainer.innerHTML = activityLogItems.map(item => item.html).join('');
        }
    }

    async function refreshStats() {
        try {
            const response = await fetch('/Admin/Dashboard?handler=Stats');
            if (!response.ok) throw new Error('Failed to fetch stats');
            
            const stats = await response.json();
            console.log('📊 Refreshing stats:', stats);
            updateStatsUI(stats);
            updateChart(stats);
            updateLastUpdateTime();

        } catch (error) {
            console.error('❌ Failed to refresh stats:', error);
        }
    }

    function updateStatsUI(stats) {
        // Update total counts
        updateElement('totalArticles', stats.totalArticles);
        updateElement('totalAccounts', stats.totalAccounts);
        updateElement('totalCategories', stats.totalCategories);
        updateElement('totalComments', stats.totalComments);

        // Update detailed stats
        updateElement('publishedArticles', stats.publishedArticles);
        updateElement('draftArticles', stats.draftArticles);
        updateElement('activeAccounts', stats.activeAccounts);
        updateElement('inactiveAccounts', stats.inactiveAccounts);
    }

    function updateElement(id, value) {
        const element = document.getElementById(id);
        if (element) {
            const oldValue = parseInt(element.textContent) || 0;
            const newValue = parseInt(value) || 0;
            if (oldValue !== newValue) {
                console.log(`📈 Updating ${id}: ${oldValue} → ${newValue}`);
                element.textContent = newValue;
                element.classList.add('stat-update');
                setTimeout(() => element.classList.remove('stat-update'), 300);
            }
        }
    }

    function updateChart(stats) {
        if (!statsChart) return;

        const newData = [
            stats.totalArticles,
            stats.totalAccounts,
            stats.totalCategories,
            stats.totalComments
        ];

        statsChart.data.datasets[0].data = newData;
        statsChart.update();
    }

    function animateStatCard(entityType) {
        const cardMap = {
            article: 'totalArticles',
            account: 'totalAccounts',
            category: 'totalCategories',
            comment: 'totalComments'
        };

        const elementId = cardMap[entityType];
        if (!elementId) return;

        const element = document.getElementById(elementId);
        if (element) {
            const card = element.closest('.card');
            if (card) {
                card.style.transform = 'scale(1.05)';
                card.style.transition = 'transform 0.3s ease';
                setTimeout(() => {
                    card.style.transform = 'scale(1)';
                }, 300);
            }
        }
    }

    function updateConnectionStatus(connected) {
        const statusBadge = document.getElementById('connectionStatus');
        if (!statusBadge) return;

        if (connected) {
            statusBadge.classList.remove('bg-danger', 'disconnected');
            statusBadge.classList.add('bg-success');
            statusBadge.innerHTML = '<i class="bi bi-wifi"></i> Connected';
        } else {
            statusBadge.classList.remove('bg-success');
            statusBadge.classList.add('bg-danger', 'disconnected');
            statusBadge.innerHTML = '<i class="bi bi-wifi-off"></i> Disconnected';
        }
    }

    function updateLastUpdateTime() {
        const element = document.getElementById('lastUpdate');
        if (element) {
            element.textContent = 'Just now';
        }
    }

    // Utility functions
    function capitalizeFirst(str) {
        return str.charAt(0).toUpperCase() + str.slice(1);
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Cleanup on page unload
    window.addEventListener('beforeunload', async function() {
        if (window.connection) {
            try {
                await window.connection.invoke('LeaveDashboardGroup');
            } catch (error) {
                console.error('Error leaving dashboard group:', error);
            }
        }
    });

})();
