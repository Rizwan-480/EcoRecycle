/* EcoRecycle Core Interactive Scripting */

document.addEventListener("DOMContentLoaded", function () {
    // 1. Theme Configuration (Dark / Light Mode)
    const storedTheme = localStorage.getItem("theme");
    const systemPreferredDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
    
    // Set initial theme
    const activeTheme = storedTheme || (systemPreferredDark ? "dark" : "light");
    document.documentElement.setAttribute("data-bs-theme", activeTheme);
    updateThemeToggleIcons(activeTheme);

    // Theme Switcher Button Event Handler
    const themeToggler = document.getElementById("theme-toggle");
    if (themeToggler) {
        themeToggler.addEventListener("click", function () {
            const currentTheme = document.documentElement.getAttribute("data-bs-theme");
            const newTheme = currentTheme === "dark" ? "light" : "dark";
            
            document.documentElement.setAttribute("data-bs-theme", newTheme);
            localStorage.setItem("theme", newTheme);
            updateThemeToggleIcons(newTheme);
        });
    }

    function updateThemeToggleIcons(theme) {
        const icon = document.querySelector("#theme-toggle i");
        if (icon) {
            if (theme === "dark") {
                icon.className = "bi bi-sun-fill";
            } else {
                icon.className = "bi bi-moon-fill";
            }
        }
    }

    // 2. Notification Live Polling via AJAX (Only if user is logged in as 'User')
    const isUserRole = document.body.dataset.role === "User";
    if (isUserRole) {
        pollNotificationCount();
        // Poll every 30 seconds
        setInterval(pollNotificationCount, 30000);
    }

    function pollNotificationCount() {
        fetch('/User/GetNotificationCount')
            .then(response => response.json())
            .then(data => {
                const badge = document.getElementById("notification-badge-el");
                if (badge) {
                    if (data.count > 0) {
                        badge.innerText = data.count;
                        badge.style.display = "inline-block";
                    } else {
                        badge.style.display = "none";
                    }
                }
            })
            .catch(err => console.error("Notification polling failed", err));
    }
});

// Toast Helper Utility
function showToastNotification(message, type = "success") {
    const toastContainer = document.getElementById("toast-container-el");
    if (!toastContainer) return;

    const toastDiv = document.createElement("div");
    toastDiv.className = `toast align-items-center text-white bg-${type === "success" ? "success" : "danger"} border-0 show`;
    toastDiv.role = "alert";
    toastDiv.ariaLive = "assertive";
    toastDiv.ariaAtomic = "true";

    toastDiv.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;

    toastContainer.appendChild(toastDiv);
    
    // Auto-remove after 5 seconds
    setTimeout(() => {
        toastDiv.classList.remove("show");
        setTimeout(() => toastDiv.remove(), 500);
    }, 5000);
}
