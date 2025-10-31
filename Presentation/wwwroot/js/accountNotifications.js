// accountNotifications.js
"use strict";


window.connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .withAutomaticReconnect()
    .build();

function debounce(fn, ms) {
    let t;
    return function () {
        clearTimeout(t);
        t = setTimeout(fn, ms);
    };
}
const debouncedReload = debounce(() => location.reload(), 300);

async function startSignalRConnection(accountId) {
    try {
        if (connection.state === "Disconnected") {
            console.log("[SR] start()");
            await connection.start();
            console.log("[SR] started. id:", connection.connectionId);
        } else {
            console.log("[SR] skip start, state:", connection.state);
        }

        // Nếu có accountId được truyền từ layout -> join group account (ACK nếu hub trả string)
        if (accountId) {
            try {
                const ack = await connection.invoke("RegisterConnection", String(accountId));
                console.log("[SR] Joined account group (on start):", ack || "(no ack)");
            } catch (e) {
                console.warn("[SR] RegisterConnection (on start) failed:", e);
            }
        }
    } catch (err) {
        console.error("[SR] start failed:", err);
        setTimeout(() => startSignalRConnection(accountId), 2000);
    }
}

// Register groups theo role + account sau khi đã Connected
async function registerRoleAndAccount() {
    if (connection.state !== "Connected") {
        console.log("[SR] wait Connected…", connection.state);
        setTimeout(registerRoleAndAccount, 200);
        return;
    }
    try {
        // Role
        if (Number.isInteger(window.userRole) && window.userRole >= 0) {
            const ackRole = await connection.invoke("RegisterUserRole", Number(window.userRole));
            console.log("[SR] Joined role group:", ackRole || "(none)");
        } else {
            console.log("[SR] No userRole -> skip");
        }

        // Account
        if (window.currentAccountId && Number(window.currentAccountId) > 0) {
            const ackAcc = await connection.invoke("RegisterConnection", String(window.currentAccountId));
            console.log("[SR] Joined account group:", ackAcc || "(none)");
        } else {
            console.log("[SR] No accountId -> skip");
        }
    } catch (e) {
        console.error("[SR] registerRoleAndAccount error:", e);
    }
}
window.startSignalRConnection = startSignalRConnection;
window.registerRoleAndAccount = registerRoleAndAccount;

// Unregister role rồi stop (dùng trước khi logout)
async function unregisterRoleThenStop() {
    try {
        if (connection.state === "Connected" &&
            Number.isInteger(window.userRole) && window.userRole >= 0) {
            await connection.invoke("UnregisterUserRole", Number(window.userRole));
            console.log("[SR] Unregistered role group.");
        } else {
            console.log("[SR] Skip UnregisterUserRole. state:", connection.state, "userRole:", window.userRole);
        }
    } catch (e) {
        console.warn("[SR] UnregisterUserRole failed:", e);
    } finally {
        try {
            if (connection.state !== "Disconnected") {
                await connection.stop();
                console.log("[SR] Stopped connection.");
            }
        } catch (err) {
            console.warn("[SR] stop failed:", err);
        }
    }
}
window.unregisterRoleThenStop = unregisterRoleThenStop;

/* =========================
   ========================= */


connection.on("ReceiveNewAccountNotification", function (message) {
    // 0=Admin, 1=Staff, 2=Lecturer (theo mapping bạn đang dùng)
    if (window.userRole === 1) {
        if (window.toastr) toastr.info(message);
        else console.log("[Toast]", message);
    }
});


connection.on("NewArticlePublished", function (message) {
    try {
        if (window.isNewsList) {
            sessionStorage.setItem('postReloadToast', message); // <-- lưu
            debouncedReload(); // ví dụ: location.reload();
            return;
        }
    } catch (e) { console.error(e); }

    toastr.info(message);
});

document.addEventListener('DOMContentLoaded', function () {
    const msg = sessionStorage.getItem('postReloadToast');
    if (msg) {
        sessionStorage.removeItem('postReloadToast');
        toastr.info(msg); 
    }
});



connection.on("UpdateNewsArticle", function (articleId) {
    try {
        const sameArticle = String(window.currentArticleId || "") === String(articleId);
        if (sameArticle || window.isNewsList) {
            debouncedReload();
        }
    } catch (e) { console.error(e); }
});

connection.on("ArticleUpdated", function (articleId, title /*, content */) {
    if (window.toastr) toastr.info(`Article updated: ${title || articleId}`);
    else console.log("[Toast] Article updated:", title || articleId);
});

connection.on("ArticleDeleted", function (articleId, title) {
    const msg = `Article deleted: ${title}  || ${articleId}`;
    if (window.toastr) toastr.warning(msg);
    else console.log("[Toast]", msg);

    console.log(window.currentArticleId);

    const sameArticle = String(window.currentArticleId || "") === String(articleId);
    if (sameArticle) window.location.href = "/";
});

connection.on("AccountDeactivated", function (accountId) {
    const currentId = parseInt(window.currentAccountId);
    const deactivatedId = parseInt(accountId);
    console.log("[SR] AccountDeactivated:", { currentId, deactivatedId });

    if (currentId === deactivatedId) {
        if (window.toastr) {
            toastr.warning("Your account has been deactivated or update.", null, {
                timeOut: 0, extendedTimeOut: 0, closeButton: true, tapToDismiss: false
            });
        }
        const form = document.createElement("form");
        form.method = "post";
        form.action = "/AccountManagement/Login?handler=Logout";
        document.body.appendChild(form);
        form.submit();
    }
});

connection.on("ForceLogout", function (payload) {
    if (window.toastr) {
        toastr.warning("You have been logged out: " + (payload?.reason || "forced"));
    } else {
        console.log("[Toast] Forced logout:", payload);
    }
    const form = document.createElement("form");
    form.method = "post";
    form.action = "/AccountManagement/Login?handler=Logout";
    document.body.appendChild(form);
    form.submit();
});

connection.on("ReceiveCreateCategoryNotification", function (message) {
    console.log("[SR] CreateCategory:", message);
    if (window.toastr) toastr.info(message);
    else console.log("[Toast]", message);
});

connection.on("ReloadCategoryList", function () {
    try {
        if (window.isCategoryList) {
            if (typeof debounce === "function") {
                debounce(() => location.reload(), 300)();
            } else {
                setTimeout(() => location.reload(), 300);
            }
        }
    } catch (e) {
        console.error("[SR] Error during ReloadCategoryList:", e);
    }
});

/* =========================
   ========================= */
connection.onreconnecting(err => console.warn("[SR] reconnecting:", err, "state:", connection.state));
connection.onreconnected(async () => {
    console.log("[SR] reconnected. id:", connection.connectionId);
    // Sau reconnect -> join lại group
    try { await registerRoleAndAccount(); } catch (e) { console.error(e); }
});
connection.onclose(async (err) => {
    console.warn("[SR] closed:", err, "state:", connection.state);
    // Tự start lại cho thân thiện (nếu còn đăng nhập)
    try {
        if (window.currentAccountId && Number(window.currentAccountId) > 0) {
            await startSignalRConnection(window.currentAccountId);
            await registerRoleAndAccount();
        }
    } catch (e) { console.error(e); }
});
