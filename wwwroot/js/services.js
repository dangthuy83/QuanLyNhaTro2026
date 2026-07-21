(() => {
    "use strict";
    const syncServiceForm = (form) => {
        const type = form?.querySelector("[name=LoaiTinhPhi]");
        const basis = form?.querySelector("[name=CachTinhCoDinh]");
        if (!type || !basis) return;
        basis.disabled = type.value !== "CoDinh";
        if (basis.disabled) basis.value = "TheoPhong";
    };
    document.querySelectorAll("#modalThemDV form, #modalSuaDV form").forEach((form) => {
        form.querySelector("[name=LoaiTinhPhi]")?.addEventListener("change", () => syncServiceForm(form));
        syncServiceForm(form);
    });
    const editModal = document.getElementById("modalSuaDV");
    document.querySelectorAll(".btn-edit-service").forEach((button) => button.addEventListener("click", () => {
        if (!editModal) return;
        const form = editModal.querySelector("form");
        form.querySelector("[name=Id]").value = button.dataset.id || "";
        form.querySelector("[name=TenDichVu]").value = button.dataset.ten || "";
        form.querySelector("[name=LoaiTinhPhi]").value = button.dataset.loai || "CoDinh";
        form.querySelector("[name=CachTinhCoDinh]").value = button.dataset.cachtinh || "TheoPhong";
        form.querySelector("[name=DonViTinh]").value = button.dataset.donvi || "";
        form.querySelector("[name=DonGiaMacDinh]").value = button.dataset.dongia || "0";
        form.querySelector("[name=BatBuocKhiThue]").checked = button.dataset.batbuoc === "true";
        syncServiceForm(form);
        bootstrap.Modal.getOrCreateInstance(editModal).show();
    }));
    const methodType = document.getElementById("loaiMoi");
    const methodBasis = document.getElementById("cachMoi");
    const openingReadings = document.getElementById("opening");
    const syncMethod = () => {
        if (!methodType || !methodBasis || !openingReadings) return;
        const metered = methodType.value === "TheoChiSo";
        methodBasis.disabled = metered;
        openingReadings.hidden = !metered;
    };
    methodType?.addEventListener("change", syncMethod);
    syncMethod();
    document.querySelectorAll(".services-page form[data-confirm]").forEach((form) => {
        form.addEventListener("submit", (event) => {
            if (!window.confirm(form.dataset.confirm || "Xác nhận thao tác?")) event.preventDefault();
        });
    });
})();
