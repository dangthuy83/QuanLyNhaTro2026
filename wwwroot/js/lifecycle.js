(() => {
    "use strict";

    const escapeText = (value) => String(value ?? "");

    document.querySelectorAll(".lifecycle-page form[data-confirm]").forEach((form) => {
        form.addEventListener("submit", (event) => {
            if (!window.confirm(form.dataset.confirm || "Xác nhận thao tác?")) event.preventDefault();
        });
    });

    const transferPage = document.querySelector('[data-lifecycle="transfer"]');
    if (!transferPage) return;

    const roomSelect = transferPage.querySelector("[name='PhongMoiId']");
    const transferDate = transferPage.querySelector("[name='NgayChuyenDi']");
    const meterLink = transferPage.querySelector("#linkNhapChiSoPhongMoi");
    const serviceContainer = transferPage.querySelector("#dichVuPhongMoi");
    const selectedIds = new Set((transferPage.dataset.selectedServiceIds || "").split(",").filter(Boolean));
    let restoreSelection = transferPage.dataset.restoreSelection === "true";

    const syncMeterLink = () => {
        const roomId = roomSelect?.value;
        const dateValue = transferDate?.value;
        if (!roomId || !dateValue || !meterLink) {
            meterLink?.setAttribute("href", "#");
            meterLink?.classList.add("disabled");
            meterLink?.setAttribute("aria-disabled", "true");
            return;
        }
        const date = new Date(`${dateValue}T00:00:00`);
        const params = new URLSearchParams({
            phongId: roomId,
            thang: String(date.getMonth() + 1),
            nam: String(date.getFullYear()),
            returnHopDongId: transferPage.dataset.oldContractId || ""
        });
        meterLink.href = `${transferPage.dataset.meterUrl}?${params}`;
        meterLink.classList.remove("disabled");
        meterLink.removeAttribute("aria-disabled");
    };

    const renderServices = (rows) => {
        if (!serviceContainer) return;
        serviceContainer.replaceChildren();
        if (!rows.length) {
            const message = document.createElement("p");
            message.className = "text-muted small mb-0";
            message.textContent = "Phòng chưa cấu hình dịch vụ.";
            serviceContainer.append(message);
            return;
        }
        rows.forEach((item) => {
            const row = document.createElement("div");
            row.className = "form-check lifecycle-service-row";
            const input = document.createElement("input");
            input.className = "form-check-input";
            input.type = "checkbox";
            input.name = "PhongDichVuIds";
            input.value = escapeText(item.id);
            input.id = `pdv_${escapeText(item.id)}`;
            input.checked = Boolean(item.batBuoc || !restoreSelection || selectedIds.has(String(item.id)));
            if (item.batBuoc) {
                input.dataset.requiredService = "true";
                input.addEventListener("click", (event) => event.preventDefault());
            }
            const label = document.createElement("label");
            label.className = "form-check-label";
            label.htmlFor = input.id;
            label.append(document.createTextNode(escapeText(item.tenDichVu)));
            if (item.batBuoc) {
                const badge = document.createElement("span");
                badge.className = "app-badge app-badge-danger ms-2";
                badge.textContent = "Bắt buộc";
                label.append(badge);
            }
            const price = document.createElement("span");
            price.className = "lifecycle-service-price";
            price.textContent = `${Number(item.donGia || 0).toLocaleString("vi-VN")} đ`;
            label.append(price);
            row.append(input, label);
            serviceContainer.append(row);
        });
    };

    const loadServices = async () => {
        const roomId = roomSelect?.value;
        if (!serviceContainer) return;
        if (!roomId) {
            serviceContainer.textContent = "Chọn phòng mới để tải dịch vụ.";
            return;
        }
        serviceContainer.setAttribute("aria-busy", "true");
        try {
            const response = await fetch(`${transferPage.dataset.serviceUrl}?phongId=${encodeURIComponent(roomId)}`);
            if (!response.ok) throw new Error("service-load");
            renderServices(await response.json());
        } catch {
            serviceContainer.textContent = "Không tải được dịch vụ. Vui lòng thử lại.";
        } finally {
            serviceContainer.removeAttribute("aria-busy");
        }
    };

    roomSelect?.addEventListener("change", () => {
        selectedIds.clear();
        restoreSelection = false;
        syncMeterLink();
        loadServices();
    });
    transferDate?.addEventListener("change", syncMeterLink);
    syncMeterLink();
    loadServices();
})();
