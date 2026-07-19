(() => {
    const sidebar = document.getElementById("appSidebar");
    const openButton = document.getElementById("drawerOpen");
    const closeButton = document.getElementById("drawerClose");
    const backdrop = document.getElementById("drawerBackdrop");
    const workspace = document.getElementById("appWorkspace");
    const bottomMore = document.getElementById("bottomMore");

    if (!sidebar || !openButton || !closeButton || !backdrop || !workspace || !bottomMore) return;

    const drawerMedia = window.matchMedia("(max-width: 1023px)");
    let returnFocus = openButton;

    const setExpanded = (expanded) => {
        openButton.setAttribute("aria-expanded", String(expanded));
        bottomMore.setAttribute("aria-expanded", String(expanded));
    };

    const setDrawer = (open, restoreFocus = true) => {
        if (!drawerMedia.matches) open = false;

        if (open && document.activeElement instanceof HTMLElement) {
            returnFocus = document.activeElement;
        }

        sidebar.classList.toggle("is-open", open);
        backdrop.classList.toggle("is-open", open);
        document.body.classList.toggle("drawer-open", open);
        setExpanded(open);

        if (drawerMedia.matches) {
            sidebar.setAttribute("aria-hidden", String(!open));
            sidebar.inert = !open;
            workspace.inert = open;
        } else {
            sidebar.removeAttribute("aria-hidden");
            sidebar.inert = false;
            workspace.inert = false;
        }

        if (open) {
            closeButton.focus();
        } else if (restoreFocus && drawerMedia.matches && returnFocus?.isConnected) {
            returnFocus.focus();
        }
    };

    const syncBreakpoint = () => setDrawer(false, false);

    openButton.addEventListener("click", () => setDrawer(true));
    bottomMore.addEventListener("click", () => setDrawer(true));
    closeButton.addEventListener("click", () => setDrawer(false));
    backdrop.addEventListener("click", () => setDrawer(false));

    sidebar.querySelectorAll("a").forEach(link => {
        link.addEventListener("click", () => setDrawer(false, false));
    });

    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && sidebar.classList.contains("is-open")) {
            event.preventDefault();
            setDrawer(false);
        }
    });

    if (drawerMedia.addEventListener) {
        drawerMedia.addEventListener("change", syncBreakpoint);
    } else {
        drawerMedia.addListener(syncBreakpoint);
    }

    syncBreakpoint();
})();
