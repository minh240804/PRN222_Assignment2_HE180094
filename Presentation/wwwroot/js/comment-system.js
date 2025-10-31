/**
 * Comment System with Markdown, Emoji, and @mention support
 * Requires: SignalR
 */

(function () {
    'use strict';

    // Initialize when DOM is ready
    window.initCommentSystem = function (config) {
        const {
            articleId,
            currentUser,
            isLoggedIn,
            isAdmin
        } = config;

        console.log('%c=== Comment System Started ===', 'background: blue; color: white; padding: 5px;');
        console.log('Article:', articleId, '| User:', currentUser, '| Logged In:', isLoggedIn, '| Admin:', isAdmin);

        if (typeof signalR === 'undefined') {
            console.error('SignalR not loaded');
            return;
        }

        async function getConnection() {
            for (let i = 0; i < 30; i++) {
                if (window.connection) {
                    console.log('✅ Connection found');
                    return window.connection;
                }
                await new Promise(r => setTimeout(r, 500));
            }
            console.error('❌ Connection timeout');
            return null;
        }

        (async function () {
            const conn = await getConnection();
            if (!conn) return;

            console.log('Connection state:', conn.state);

            // SignalR event handlers
            conn.on("ReceiveComment", (data) => {
                console.log('💬 New comment from:', data.user, '| Message:', data.message.substring(0, 50));
                addComment(data.commentId, data.user, data.message, data.timestamp);
            });

            conn.on("CommentDeleted", (data) => {
                showAlert(data.reason, 'warning');
                removeComment(data.commentId);
            });

            conn.on("CommentRemovedFromArticle", (data) => {
                removeComment(data.commentId);
            });

            // Setup form submission for logged-in non-admin users
            if (isLoggedIn && !isAdmin) {
                const form = document.getElementById('commentForm');
                const input = document.getElementById('commentMessage');

                if (form) {
                    form.addEventListener('submit', async (e) => {
                        e.preventDefault();
                        await postComment();
                    });
                }

                if (input) {
                    input.addEventListener('keypress', (e) => {
                        if (e.key === 'Enter' && !e.shiftKey) {
                            e.preventDefault();
                            form?.dispatchEvent(new Event('submit'));
                        }
                    });
                }
            }

            // Setup delete buttons for admins
            if (isAdmin) {
                document.addEventListener('click', async (e) => {
                    const btn = e.target.closest('.delete-comment-btn');
                    if (btn && confirm('Delete this comment?')) {
                        await deleteComment(btn.dataset.commentId);
                    }
                });
            }

            // Post comment function
            async function postComment() {
                const input = document.getElementById('commentMessage');
                const btn = document.getElementById('sendCommentBtn');
                const msg = input?.value.trim();

                if (!msg) {
                    showAlert('Please enter a comment', 'warning');
                    return;
                }

                if (btn) {
                    btn.disabled = true;
                    btn.innerHTML = '<i class="bi bi-hourglass-split"></i> Sending...';
                }

                try {
                    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                    const res = await fetch('?handler=SendComment', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/x-www-form-urlencoded',
                            'RequestVerificationToken': token
                        },
                        body: new URLSearchParams({
                            articleId,
                            message: msg,
                            __RequestVerificationToken: token
                        })
                    });

                    const result = await res.json();

                    if (result.success) {
                        input.value = '';
                        console.log('✅ Comment posted successfully');
                        showAlert('Comment posted!', 'success');
                    } else {
                        console.error('❌ Post failed:', result.error);
                        showAlert(result.error || 'Failed', 'error');
                    }
                } catch (err) {
                    console.error('❌ Post error:', err);
                    showAlert('Failed to post', 'error');
                } finally {
                    if (btn) {
                        btn.disabled = false;
                        btn.innerHTML = '<i class="bi bi-send"></i> Send';
                    }
                }
            }

            // Delete comment function
            async function deleteComment(id) {
                try {
                    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

                    console.log('🗑️ Deleting comment:', id);

                    const res = await fetch(`?handler=DeleteComment`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/x-www-form-urlencoded',
                            'RequestVerificationToken': token
                        },
                        body: new URLSearchParams({
                            commentId: id,
                            articleId: articleId,
                            __RequestVerificationToken: token
                        })
                    });

                    console.log('Response status:', res.status);

                    if (!res.ok) {
                        const errorText = await res.text();
                        console.error('Response error:', errorText);
                        throw new Error(`HTTP ${res.status}`);
                    }

                    const result = await res.json();
                    console.log('Delete result:', result);

                    if (result.success) {
                        showAlert('Comment deleted successfully', 'success');
                    } else {
                        showAlert(result.error || 'Failed to delete', 'error');
                    }
                } catch (err) {
                    console.error('❌ Delete error:', err);
                    showAlert('Failed to delete: ' + err.message, 'error');
                }
            }

            // Add comment to UI
            function addComment(commentId, user, msg, time) {
                const list = document.getElementById('commentsList');
                const placeholder = list?.querySelector('.text-muted.text-center');
                if (placeholder) placeholder.remove();

                const div = document.createElement('div');
                div.className = 'mb-3 p-3 bg-white rounded shadow-sm';
                div.id = `comment-${commentId}`;

                const delBtn = isAdmin ? `
                    <button class="btn btn-sm btn-outline-danger delete-comment-btn ms-2" 
                            data-comment-id="${commentId}">
                        <i class="bi bi-trash"></i>
                    </button>` : '';

                div.innerHTML = `
                    <div class="d-flex justify-content-between align-items-start mb-2">
                        <strong class="text-primary">
                            <i class="bi bi-person-circle"></i> ${esc(user)}
                        </strong>
                        <div>
                            <small class="text-muted me-2">
                                <i class="bi bi-clock"></i> ${esc(time)}
                            </small>
                            ${delBtn}
                        </div>
                    </div>
                    <div class="comment-text">${formatComment(msg)}</div>
                `;

                list?.prepend(div);
                div.style.opacity = '0';
                setTimeout(() => {
                    div.style.transition = 'opacity 0.3s';
                    div.style.opacity = '1';
                }, 10);
            }

            // Remove comment from UI
            function removeComment(id) {
                const elem = document.getElementById(`comment-${id}`);
                if (elem) {
                    elem.style.transition = 'opacity 0.3s';
                    elem.style.opacity = '0';
                    setTimeout(() => elem.remove(), 300);
                }
            }

            // Escape HTML to prevent XSS
            function esc(text) {
                const div = document.createElement('div');
                div.textContent = text;
                return div.innerHTML;
            }

            // Format comment with markdown and mentions
            function formatComment(text) {
                // First escape HTML to prevent XSS
                let formatted = esc(text);

                // Process @mentions
                formatted = formatted.replace(/@(\w+)/g, '<span class="mention">@$1</span>');

                // Process inline code `code`
                formatted = formatted.replace(/`([^`]+)`/g, '<code>$1</code>');

                // Process bold **text**
                formatted = formatted.replace(/\*\*([^\*]+)\*\*/g, '<strong>$1</strong>');

                // Process italic *text*
                formatted = formatted.replace(/\*([^\*]+)\*/g, '<em>$1</em>');

                // Process links [text](url)
                formatted = formatted.replace(/\[([^\]]+)\]\(([^\)]+)\)/g, '<a href="$2" target="_blank">$1</a>');

                // Emojis are already supported natively!

                return formatted;
            }

            // Show alert notification
            function showAlert(msg, type = 'info') {
                if (typeof toastr !== 'undefined' && toastr[type]) {
                    toastr[type](msg);
                } else {
                    const colors = { success: 'success', error: 'danger', warning: 'warning', info: 'info' };
                    const a = document.createElement('div');
                    a.className = `alert alert-${colors[type] || 'info'} alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3`;
                    a.style.zIndex = '9999';
                    a.style.minWidth = '300px';
                    a.innerHTML = `${msg}<button type="button" class="btn-close" data-bs-dismiss="alert"></button>`;
                    document.body.appendChild(a);
                    setTimeout(() => a.remove(), 5000);
                }
            }

            // Join SignalR group for this article
            async function joinGroup() {
                try {
                    // Wait for connection to be ready
                    if (conn.state !== signalR.HubConnectionState.Connected) {
                        console.log('Waiting for connection... Current state:', conn.state);

                        // Wait up to 10 seconds for connection
                        for (let i = 0; i < 20; i++) {
                            if (conn.state === signalR.HubConnectionState.Connected) {
                                break;
                            }
                            await new Promise(r => setTimeout(r, 500));
                        }
                    }

                    if (conn.state === signalR.HubConnectionState.Connected) {
                        await conn.invoke("JoinArticleGroup", articleId);
                        console.log('%c✓ Joined article group: ' + articleId, 'color: green; font-weight: bold;');
                    } else {
                        console.error('❌ Connection not ready. State:', conn.state);
                        showAlert('Live comments unavailable', 'warning');
                    }
                } catch (err) {
                    console.error('❌ Join error:', err);
                    showAlert('Could not join live comments', 'warning');
                }
            }

            // Leave group on page unload
            window.addEventListener('beforeunload', () => {
                if (conn.state === signalR.HubConnectionState.Connected) {
                    conn.invoke("LeaveArticleGroup", articleId).catch(console.error);
                }
            });

            // Format existing comments on page load
            document.querySelectorAll('.comment-text').forEach(element => {
                const originalText = element.textContent;
                element.innerHTML = formatComment(originalText);
            });

            await joinGroup();
        })();
    };
})();
