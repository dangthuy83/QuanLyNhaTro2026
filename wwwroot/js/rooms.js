(() => {
    "use strict";

    const currencyFormatter = new Intl.NumberFormat("vi-VN", {
        maximumFractionDigits: 0
    });

    const normalizeText = (value) => (value || "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLocaleLowerCase("vi-VN")
        .trim();

    const formatCurrency = (value) => `${currencyFormatter.format(Number.isFinite(value) ? value : 0)} đ`;

    document.querySelectorAll("[data-money-preview]").forEach((input) => {
        const refresh = () => {
            const preview = document.getElementById(input.dataset.moneyPreview);
            if (!preview) return;
            preview.textContent = formatCurrency(Number(input.value));
        };

        input.addEventListener("input", refresh);
        refresh();
    });

    document.querySelectorAll("[data-service-toggle]").forEach((checkbox) => {
        const syncPriceState = () => {
            const priceInput = document.getElementById(checkbox.dataset.priceTarget);
            const row = checkbox.closest("[data-service-row]");
            if (!priceInput) return;

            priceInput.disabled = !checkbox.checked;
            row?.classList.toggle("is-disabled", !checkbox.checked);
        };

        checkbox.addEventListener("change", syncPriceState);
        syncPriceState();
    });

    const listRoot = document.querySelector("[data-rooms-list]");
    if (listRoot) {
        const searchInput = listRoot.querySelector("[data-room-filter-search]");
        const houseSelect = listRoot.querySelector("[data-room-filter-house]");
        const statusSelect = listRoot.querySelector("[data-room-filter-status]");
        const resetButton = listRoot.querySelector("[data-room-filter-reset]");
        const rows = Array.from(listRoot.querySelectorAll("[data-room-row]"));
        const resultCount = listRoot.querySelector("[data-room-result-count]");
        const filteredEmpty = listRoot.querySelector("[data-room-filter-empty]");

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

            if (resultCount) resultCount.textContent = `${visibleCount} phòng`;
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

    const bulkRoot = document.querySelector("[data-bulk-assignment]");
    if (bulkRoot) {
        const checkAll = bulkRoot.querySelector("[data-check-all]");
        const roomChecks = Array.from(bulkRoot.querySelectorAll("[data-room-check]"));
        const priceInput = bulkRoot.querySelector("[name=donGia]");
        const submitButton = bulkRoot.querySelector("[data-bulk-submit]");
        const selectionCount = bulkRoot.querySelector("[data-selection-count]");

        const refreshSelection = () => {
            const selected = roomChecks.filter((checkbox) => checkbox.checked).length;
            if (checkAll) {
                checkAll.checked = roomChecks.length > 0 && selected === roomChecks.length;
                checkAll.indeterminate = selected > 0 && selected < roomChecks.length;
            }
            if (submitButton) submitButton.disabled = selected === 0;
            if (selectionCount) selectionCount.textContent = `${selected} phòng đã chọn`;
        };

        const refreshAmounts = () => {
            const price = Number(priceInput?.value || 0);
            bulkRoot.querySelectorAll("[data-preview-amount]").forEach((element) => {
                const quantity = Number(element.dataset.quantity || 0);
                const fixedPrice = element.dataset.fixedPrice === undefined || element.dataset.fixedPrice === ""
                    ? Number.NaN
                    : Number(element.dataset.fixedPrice);
                const unitPrice = Number.isFinite(fixedPrice) ? fixedPrice : price;
                element.textContent = formatCurrency(quantity * unitPrice);
            });
            bulkRoot.querySelectorAll("[data-preview-unit-price]").forEach((element) => {
                const fixedPrice = element.dataset.fixedPrice === undefined || element.dataset.fixedPrice === ""
                    ? Number.NaN
                    : Number(element.dataset.fixedPrice);
                element.textContent = currencyFormatter.format(Number.isFinite(fixedPrice) ? fixedPrice : price);
            });
        };

        checkAll?.addEventListener("change", () => {
            roomChecks.forEach((checkbox) => {
                checkbox.checked = checkAll.checked;
            });
            refreshSelection();
        });
        roomChecks.forEach((checkbox) => checkbox.addEventListener("change", refreshSelection));
        priceInput?.addEventListener("input", refreshAmounts);

        refreshSelection();
        refreshAmounts();
    }
})();
