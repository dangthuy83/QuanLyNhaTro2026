(() => {
    "use strict";

    const normalize = (value) => (value || "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLocaleLowerCase("vi");

    const list = document.querySelector("[data-invoice-list]");
    if (list) {
        const search = list.querySelector("[data-invoice-filter-search]");
        const house = list.querySelector("[data-invoice-filter-house]");
        const status = list.querySelector("[data-invoice-filter-status]");
        const reset = list.querySelector("[data-invoice-filter-reset]");
        const rows = Array.from(list.querySelectorAll("[data-invoice-row]"));
        const count = list.querySelector("[data-invoice-result-count]");
        const empty = list.querySelector("[data-invoice-filter-empty]");

        const applyFilters = () => {
            const term = normalize(search?.value);
            const houseValue = house?.value || "";
            const statusValue = status?.value || "";
            let visible = 0;

            rows.forEach((row) => {
                const matchesSearch = !term || normalize(row.dataset.search).includes(term);
                const matchesHouse = !houseValue || row.dataset.house === houseValue;
                const matchesStatus = !statusValue
                    || (statusValue === "QuaHan" ? row.dataset.overdue === "true" : row.dataset.status === statusValue);
                const show = matchesSearch && matchesHouse && matchesStatus;
                row.hidden = !show;
                if (show) visible += 1;
            });

            if (count) count.textContent = `${visible} hóa đơn`;
            if (empty) empty.hidden = visible !== 0;
        };

        [search, house, status].forEach((control) => {
            control?.addEventListener(control === search ? "input" : "change", applyFilters);
        });
        reset?.addEventListener("click", () => {
            if (search) search.value = "";
            if (house) house.value = "";
            if (status) status.value = "";
            applyFilters();
            search?.focus();
        });
    }

    document.querySelectorAll("[data-quick-fill]").forEach((button) => {
        button.addEventListener("click", () => {
            const input = button.closest("form")?.querySelector("input[name='soTien']");
            if (!input) return;
            input.value = button.dataset.amount || input.max;
            input.focus();
            input.select();
        });
    });

    document.querySelectorAll("[data-invoice-delete-form]").forEach((form) => {
        form.addEventListener("submit", (event) => {
            if (!window.confirm(form.dataset.confirmMessage || "Xóa hóa đơn này?")) {
                event.preventDefault();
            }
        });
    });

    const bulk = document.querySelector("[data-invoice-bulk]");
    if (bulk) {
        const checkAll = bulk.querySelector("[data-bulk-check-all]");
        const ready = Array.from(bulk.querySelectorAll(".ready-check:not(:disabled)"));
        const output = bulk.querySelector("[data-bulk-selected-count]");
        const submit = bulk.querySelector("form[method='post'] button[type='submit']");

        const updateSelection = () => {
            const selected = ready.filter((checkbox) => checkbox.checked).length;
            if (output) output.textContent = `${selected} dòng đã chọn`;
            if (submit) submit.disabled = selected === 0;
            if (checkAll) {
                checkAll.checked = ready.length > 0 && selected === ready.length;
                checkAll.indeterminate = selected > 0 && selected < ready.length;
            }
        };

        checkAll?.addEventListener("change", () => {
            ready.forEach((checkbox) => { checkbox.checked = checkAll.checked; });
            updateSelection();
        });
        ready.forEach((checkbox) => checkbox.addEventListener("change", updateSelection));
        updateSelection();
    }

    document.querySelector("[data-print-receipt]")?.addEventListener("click", () => window.print());
})();
