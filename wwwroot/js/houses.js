(() => {
    "use strict";
    const modalElement = document.getElementById("modalSuaNha");
    document.querySelectorAll(".btn-edit-house").forEach((button) => button.addEventListener("click", () => {
        if (!modalElement) return;
        modalElement.querySelector("[name=Id]").value = button.dataset.id || "";
        modalElement.querySelector("[name=TenNha]").value = button.dataset.ten || "";
        modalElement.querySelector("[name=DiaChi]").value = button.dataset.diachi || "";
        modalElement.querySelector("[name=GhiChu]").value = button.dataset.ghichu || "";
        bootstrap.Modal.getOrCreateInstance(modalElement).show();
    }));
    document.querySelectorAll("form[data-confirm]").forEach((form) => form.addEventListener("submit", (event) => {
        if (!window.confirm(form.dataset.confirm || "Xác nhận thao tác?")) event.preventDefault();
    }));
})();
