(() => {
    "use strict";

    const normalizeText = (value) => (value || "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLocaleLowerCase("vi-VN")
        .trim();

    const escapeHtml = (value) => String(value ?? "").replace(/[&<>'"]/g, character => ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        "'": "&#39;",
        "\"": "&quot;"
    }[character]));

    const moneyFormatter = new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 });
    const formatMoney = (value) => `${moneyFormatter.format(Number(value) || 0)} đ`;

    const listRoot = document.querySelector("[data-contracts-list]");
    if (listRoot) {
        const searchInput = listRoot.querySelector("[data-contract-filter-search]");
        const houseSelect = listRoot.querySelector("[data-contract-filter-house]");
        const statusSelect = listRoot.querySelector("[data-contract-filter-status]");
        const resetButton = listRoot.querySelector("[data-contract-filter-reset]");
        const rows = Array.from(listRoot.querySelectorAll("[data-contract-row]"));
        const resultCount = listRoot.querySelector("[data-contract-result-count]");
        const filteredEmpty = listRoot.querySelector("[data-contract-filter-empty]");

        const applyFilters = () => {
            const query = normalizeText(searchInput?.value);
            const house = houseSelect?.value || "";
            const status = statusSelect?.value || "";
            let visibleCount = 0;

            rows.forEach((row) => {
                const matchesSearch = !query || normalizeText(row.dataset.search).includes(query);
                const matchesHouse = !house || row.dataset.house === house;
                const matchesStatus = !status || row.dataset.status === status;
                const visible = matchesSearch && matchesHouse && matchesStatus;
                row.hidden = !visible;
                visibleCount += visible ? 1 : 0;
            });

            if (resultCount) resultCount.textContent = `${visibleCount} hợp đồng`;
            if (filteredEmpty) filteredEmpty.hidden = visibleCount > 0 || rows.length === 0;
        };

        searchInput?.addEventListener("input", applyFilters);
        houseSelect?.addEventListener("change", applyFilters);
        statusSelect?.addEventListener("change", applyFilters);
        resetButton?.addEventListener("click", () => {
            if (searchInput) searchInput.value = "";
            if (houseSelect) houseSelect.value = "";
            if (statusSelect) statusSelect.value = "";
            applyFilters();
            searchInput?.focus();
        });
        applyFilters();
    }

    document.querySelectorAll("[data-contract-money-preview]").forEach((input) => {
        const update = () => {
            const preview = document.getElementById(input.dataset.contractMoneyPreview);
            if (preview) preview.textContent = formatMoney(input.value);
        };
        input.addEventListener("input", update);
        update();
    });

    const createRoot = document.querySelector("[data-contract-create]");
    if (createRoot) {
        const roomSelect = createRoot.querySelector("[name='PhongId']");
        const services = createRoot.querySelector("[data-contract-services]");
        const rentInput = createRoot.querySelector("[name='TienThueThoaThuan']");
        const depositInput = createRoot.querySelector("[name='TienCoc']");
        const servicesUrl = createRoot.dataset.servicesUrl;
        const tenantSearchUrl = createRoot.dataset.tenantSearchUrl;
        let restoreSelection = createRoot.dataset.restoreSelection === "true";
        const selectedIds = new Set(JSON.parse(createRoot.dataset.selectedServiceIds || "[]").map(Number));

        const bindRequiredServices = () => {
            services?.querySelectorAll('[data-required-service="true"]').forEach((checkbox) => {
                checkbox.addEventListener("click", event => event.preventDefault());
            });
        };

        const loadServices = async () => {
            const roomId = roomSelect?.value;
            if (!services) return;
            if (!roomId) {
                services.innerHTML = '<div class="contracts-inline-empty"><svg class="app-icon" aria-hidden="true"><use href="#icon-service"></use></svg><span>Chọn phòng để tải dịch vụ có thể đăng ký.</span></div>';
                return;
            }

            services.setAttribute("aria-busy", "true");
            try {
                const response = await fetch(`${servicesUrl}?phongId=${encodeURIComponent(roomId)}`);
                if (!response.ok) throw new Error("service-request-failed");
                const rows = await response.json();
                if (!rows.length) {
                    services.innerHTML = '<div class="contracts-inline-empty is-warning"><svg class="app-icon" aria-hidden="true"><use href="#icon-alert"></use></svg><span>Phòng chưa được cấu hình dịch vụ.</span></div>';
                    return;
                }

                services.innerHTML = rows.map(item => {
                    const checked = item.batBuoc || !restoreSelection || selectedIds.has(Number(item.id));
                    const calculation = [item.loaiTinhPhi, item.cachTinh].filter(Boolean).join(" · ");
                    return `<div class="contracts-service-option">
                        <div class="contracts-service-option-main">
                            <input class="form-check-input" type="checkbox" name="phongDichVuIds" value="${Number(item.id)}" id="pdv_${Number(item.id)}" ${checked ? "checked" : ""} ${item.batBuoc ? 'data-required-service="true" aria-describedby="required-service-help"' : ""}>
                            <label class="form-check-label" for="pdv_${Number(item.id)}">
                                <span class="contracts-service-option-title">${escapeHtml(item.tenDichVu)}${item.batBuoc ? '<span class="app-status-badge is-danger"><span class="app-status-dot" aria-hidden="true"></span>Bắt buộc</span>' : ""}</span>
                                <span class="contracts-service-option-meta">${escapeHtml(calculation || "Chưa xác định cách tính")}</span>
                            </label>
                        </div>
                        <strong class="contracts-service-option-price">${formatMoney(item.donGia)}</strong>
                    </div>`;
                }).join("");
                bindRequiredServices();
            } catch {
                services.innerHTML = '<div class="contracts-inline-empty is-danger"><svg class="app-icon" aria-hidden="true"><use href="#icon-alert"></use></svg><span>Không tải được danh sách dịch vụ. Hãy thử chọn lại phòng.</span></div>';
            } finally {
                services.removeAttribute("aria-busy");
            }
        };

        const applyRoomPrice = () => {
            const price = roomSelect?.selectedOptions[0]?.dataset.price;
            if (price === undefined) return;
            if (rentInput) rentInput.value = price;
            if (depositInput) depositInput.value = price;
            rentInput?.dispatchEvent(new Event("input"));
            depositInput?.dispatchEvent(new Event("input"));
        };

        roomSelect?.addEventListener("change", () => {
            selectedIds.clear();
            restoreSelection = false;
            applyRoomPrice();
            loadServices();
        });
        if (!restoreSelection && roomSelect?.value) applyRoomPrice();
        loadServices();

        const searchInput = createRoot.querySelector("[data-tenant-search]");
        const searchResults = createRoot.querySelector("[data-tenant-results]");
        const selectedTenants = createRoot.querySelector("[data-selected-tenants]");
        const selectedEmpty = createRoot.querySelector("[data-selected-tenants-empty]");
        let searchTimer;

        const syncSelectedState = () => {
            if (selectedEmpty && selectedTenants) {
                selectedEmpty.hidden = selectedTenants.querySelectorAll("[data-tenant-row]").length > 0;
            }
        };

        const bindRemoveButtons = () => selectedTenants?.querySelectorAll("[data-remove-tenant]").forEach((button) => {
            button.addEventListener("click", () => {
                button.closest("[data-tenant-row]")?.remove();
                syncSelectedState();
            }, { once: true });
        });

        const addTenant = (item) => {
            if (!selectedTenants || selectedTenants.querySelector(`[data-tenant-id="${Number(item.id)}"]`)) return;
            const shouldRepresent = !selectedTenants.querySelector("input[name='khachChinhId']:checked");
            const row = document.createElement("div");
            row.className = "contracts-tenant-row";
            row.dataset.tenantRow = "";
            row.dataset.tenantId = String(Number(item.id));
            row.innerHTML = `<input type="hidden" name="khachThueIds" value="${Number(item.id)}">
                <label class="contracts-representative-choice">
                    <input type="radio" name="khachChinhId" value="${Number(item.id)}" ${shouldRepresent ? "checked" : ""}>
                    <span><strong>${escapeHtml(item.hoTen)}</strong><small>${escapeHtml(item.soDienThoai || "Chưa có SĐT")} · CCCD ${escapeHtml(item.cccd || "chưa có")}</small></span>
                </label>
                <button type="button" class="btn btn-outline-danger" data-remove-tenant aria-label="Bỏ ${escapeHtml(item.hoTen)} khỏi hợp đồng">Bỏ</button>`;
            selectedTenants.appendChild(row);
            bindRemoveButtons();
            syncSelectedState();
        };

        searchInput?.addEventListener("input", () => {
            clearTimeout(searchTimer);
            const term = searchInput.value.trim();
            if (!searchResults) return;
            if (term.length < 2) {
                searchResults.innerHTML = "";
                searchResults.hidden = true;
                return;
            }
            searchTimer = setTimeout(async () => {
                try {
                    const response = await fetch(`${tenantSearchUrl}?term=${encodeURIComponent(term)}&limit=20`);
                    const rows = response.ok ? await response.json() : [];
                    searchResults.hidden = false;
                    searchResults.innerHTML = rows.length
                        ? rows.map((item, index) => `<button type="button" class="contracts-search-result" data-result-index="${index}"><strong>${escapeHtml(item.hoTen)}</strong><span>${escapeHtml(item.soDienThoai || "Chưa có SĐT")} · CCCD ${escapeHtml(item.cccd || "chưa có")} · ${escapeHtml(item.bienSoXe || "chưa có biển số")}</span></button>`).join("")
                        : '<div class="contracts-search-empty">Không tìm thấy hồ sơ phù hợp.</div>';
                    searchResults.querySelectorAll("[data-result-index]").forEach((button) => {
                        button.addEventListener("click", () => {
                            addTenant(rows[Number(button.dataset.resultIndex)]);
                            searchResults.innerHTML = "";
                            searchResults.hidden = true;
                            searchInput.value = "";
                            searchInput.focus();
                        });
                    });
                } catch {
                    searchResults.hidden = false;
                    searchResults.innerHTML = '<div class="contracts-search-empty is-danger">Không thể tìm hồ sơ lúc này.</div>';
                }
            }, 300);
        });
        bindRemoveButtons();
        syncSelectedState();
    }

    const residentRoot = document.querySelector("[data-resident-form]");
    if (residentRoot) {
        const searchInput = residentRoot.querySelector("[data-resident-search]");
        const idInput = residentRoot.querySelector("[name='khachThueId']");
        const results = residentRoot.querySelector("[data-resident-results]");
        const selected = residentRoot.querySelector("[data-resident-selected]");
        const searchUrl = residentRoot.dataset.tenantSearchUrl;
        let timer;

        searchInput?.addEventListener("input", () => {
            clearTimeout(timer);
            if (idInput) idInput.value = "";
            if (selected) selected.textContent = "Chưa chọn hồ sơ.";
            const term = searchInput.value.trim();
            if (!results) return;
            if (term.length < 2) {
                results.innerHTML = "";
                results.hidden = true;
                return;
            }
            timer = setTimeout(async () => {
                const response = await fetch(`${searchUrl}?term=${encodeURIComponent(term)}&limit=20`);
                const rows = response.ok ? await response.json() : [];
                results.hidden = false;
                results.innerHTML = rows.length
                    ? rows.map((item, index) => `<button type="button" class="contracts-search-result" data-resident-index="${index}"><strong>${escapeHtml(item.hoTen)}</strong><span>${escapeHtml(item.soDienThoai || "Chưa có SĐT")} · CCCD ${escapeHtml(item.cccd || "chưa có")} · ${escapeHtml(item.bienSoXe || "chưa có biển số")}</span></button>`).join("")
                    : '<div class="contracts-search-empty">Không tìm thấy hồ sơ phù hợp.</div>';
                results.querySelectorAll("[data-resident-index]").forEach((button) => {
                    button.addEventListener("click", () => {
                        const row = rows[Number(button.dataset.residentIndex)];
                        if (idInput) idInput.value = row.id;
                        searchInput.value = row.hoTen;
                        if (selected) selected.textContent = `Đã chọn hồ sơ #${row.id} · ${row.soDienThoai || "chưa có SĐT"}`;
                        results.innerHTML = "";
                        results.hidden = true;
                    });
                });
            }, 300);
        });
    }

    document.querySelectorAll('[data-required-service="true"]').forEach((checkbox) => {
        checkbox.addEventListener("click", event => event.preventDefault());
    });

    const serviceForm = document.querySelector("[data-contract-service-form]");
    if (serviceForm) {
        const countOutput = serviceForm.closest(".contracts-service-panel")?.querySelector("[data-selected-service-count]");
        const updateCount = () => {
            if (!countOutput) return;
            const count = serviceForm.querySelectorAll("input[name='PhongDichVuIds']:checked").length;
            countOutput.textContent = `${count} dịch vụ được chọn`;
        };
        serviceForm.querySelectorAll("input[name='PhongDichVuIds']").forEach((checkbox) => checkbox.addEventListener("change", updateCount));
        updateCount();
    }

    const cancelForm = document.querySelector("[data-contract-cancel-form]");
    cancelForm?.addEventListener("submit", (event) => {
        const message = cancelForm.dataset.confirmMessage || "Xác nhận hủy hợp đồng?";
        if (!window.confirm(message)) event.preventDefault();
    });
})();
