(() => {
    "use strict";

    const parseReading = value => {
        const number = Number.parseFloat(value);
        return Number.isFinite(number) ? number : null;
    };

    const formatReading = value => new Intl.NumberFormat("vi-VN", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(value);

    const updateMeterRow = row => {
        const type = row.querySelector("[data-meter-type]");
        const resetFields = row.querySelector("[data-reset-fields]");
        const startInput = row.querySelector("[data-start-input]");
        const endInput = row.querySelector("[data-meter-end-input]");
        const preview = row.querySelector("[data-meter-consumption]");
        const isReset = type?.value === "Reset";
        const start = parseReading(startInput?.value ?? row.dataset.chiSoDau ?? "0");
        const end = parseReading(endInput?.value ?? "");

        resetFields?.classList.toggle("d-none", !isReset);
        if (endInput && start !== null) endInput.min = isReset ? "0" : String(start);

        resetFields?.querySelectorAll("input").forEach(input => {
            const isReason = input.name.toLowerCase().includes("lydo");
            input.required = isReset && (isReason || input.name.toLowerCase().includes("truocreset") || input.name.toLowerCase().includes("saureset"));
        });

        if (!preview || start === null || end === null) return;
        let consumption = end - start;
        if (isReset) {
            const before = parseReading(resetFields?.querySelector("input[name*='TruocReset'], input[name*='truocReset']")?.value ?? "");
            const after = parseReading(resetFields?.querySelector("input[name*='SauReset'], input[name*='sauReset']")?.value ?? "0");
            consumption = before === null || after === null ? Number.NaN : (before - start) + (end - after);
        }
        preview.textContent = Number.isFinite(consumption) && consumption >= 0 ? formatReading(consumption) : "—";
    };

    document.querySelectorAll("[data-meter-row]").forEach(row => {
        row.querySelectorAll("input, select").forEach(control => {
            control.addEventListener(control.tagName === "SELECT" ? "change" : "input", () => updateMeterRow(row));
        });
        updateMeterRow(row);
    });

    document.querySelectorAll("[data-meter-overview], [data-meter-bulk]").forEach(scope => {
        const rows = [...scope.querySelectorAll("[data-meter-overview-row], [data-meter-bulk-row]")];
        const house = scope.querySelector("[data-meter-filter='house']");
        const status = scope.querySelector("[data-meter-filter='status']");
        const search = scope.querySelector("[data-meter-filter='search']");
        const result = scope.querySelector("[data-meter-result-count]");
        const empty = scope.querySelector("[data-meter-filter-empty]");

        const applyFilters = () => {
            const query = (search?.value ?? "").trim().toLocaleLowerCase("vi");
            let visible = 0;
            rows.forEach(row => {
                const show = (!house?.value || row.dataset.house === house.value)
                    && (!status?.value || row.dataset.status === status.value)
                    && (!query || (row.dataset.search ?? "").includes(query));
                row.hidden = !show;
                if (show) visible += 1;
            });
            if (result) result.textContent = `${visible} dòng`;
            empty?.classList.toggle("d-none", visible !== 0);
            updateSelection();
        };

        const updateSelection = () => {
            const selected = rows.filter(row => !row.hidden && row.querySelector("[data-meter-select-row]")?.checked).length;
            const count = scope.querySelector("[data-meter-selected-count]");
            const save = scope.querySelector("[data-meter-save-selected]");
            const selectAll = scope.querySelector("[data-meter-select-all]");
            if (count) count.textContent = String(selected);
            if (save) save.disabled = selected === 0;
            if (selectAll) {
                const available = rows.filter(row => !row.hidden && row.querySelector("[data-meter-select-row]"));
                selectAll.checked = available.length > 0 && available.every(row => row.querySelector("[data-meter-select-row]").checked);
                selectAll.indeterminate = selected > 0 && !selectAll.checked;
            }
        };

        [house, status, search].forEach(control => control?.addEventListener(control === search ? "input" : "change", applyFilters));
        scope.querySelector("[data-meter-reset-filter]")?.addEventListener("click", () => {
            if (house) house.value = "";
            if (status) status.value = "";
            if (search) search.value = "";
            applyFilters();
            search?.focus();
        });
        scope.querySelectorAll("[data-meter-select-row]").forEach(box => box.addEventListener("change", updateSelection));
        scope.querySelector("[data-meter-select-all]")?.addEventListener("change", event => {
            rows.filter(row => !row.hidden).forEach(row => {
                const box = row.querySelector("[data-meter-select-row]");
                if (box) box.checked = event.currentTarget.checked;
            });
            updateSelection();
        });
        applyFilters();
    });

    document.querySelectorAll("[data-meter-delete-form]").forEach(form => {
        form.addEventListener("submit", event => {
            const label = form.dataset.meterDeleteLabel ?? "mốc này";
            if (!window.confirm(`Xóa ${label}? Server sẽ chặn nếu mốc đang được dữ liệu sau sử dụng.`)) event.preventDefault();
        });
    });
})();
