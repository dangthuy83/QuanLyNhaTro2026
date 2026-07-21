(() => {
    "use strict";

    document.querySelectorAll("[data-photo-input]").forEach((input) => {
        input.addEventListener("change", () => {
            const preview = document.getElementById(input.dataset.previewTarget || "");
            const file = input.files?.[0];
            if (!preview || !file) return;
            if (preview.dataset.objectUrl) URL.revokeObjectURL(preview.dataset.objectUrl);
            const objectUrl = URL.createObjectURL(file);
            if (preview instanceof HTMLImageElement) {
                preview.dataset.objectUrl = objectUrl;
                preview.src = objectUrl;
                preview.hidden = false;
                return;
            }
            const image = document.createElement("img");
            image.id = preview.id;
            image.className = preview.className;
            image.alt = preview.dataset.previewAlt || "Xem trước ảnh đã chọn";
            image.dataset.objectUrl = objectUrl;
            image.src = objectUrl;
            preview.replaceWith(image);
        });
    });

    document.querySelectorAll("form[data-confirm]").forEach((form) => {
        form.addEventListener("submit", (event) => {
            if (!window.confirm(form.dataset.confirm || "Xác nhận thao tác?")) event.preventDefault();
        });
    });
})();
