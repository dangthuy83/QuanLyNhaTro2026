(() => {
    "use strict";
    const type = document.getElementById("loaiGiaoDich");
    const method = document.getElementById("phuongThuc");
    const invoice = document.getElementById("hoaDonId");
    const sync = () => {
        if (!type || !method || !invoice) return;
        const nonCash = type.value === "TruNo";
        invoice.disabled = !nonCash;
        invoice.required = nonCash;
        method.disabled = nonCash;
        method.required = !nonCash;
        if (!nonCash) invoice.value = "";
        if (nonCash) method.value = "";
    };
    type?.addEventListener("change", sync);
    sync();
    document.querySelectorAll("form[data-confirm]").forEach((form) => form.addEventListener("submit", (event) => {
        if (!window.confirm(form.dataset.confirm || "Xác nhận thao tác?")) event.preventDefault();
    }));
})();
