// Cascading dropdown Tỉnh/Thành phố -> Xã/Phường, dùng API mở provinces.open-api.vn/api/v2
// (bản v2 phản ánh đúng cấu trúc hành chính 2 cấp từ 01/07/2025, không còn Quận/Huyện).
//
// Cách dùng: gọi initAddressCascade({ provinceSelectId, wardSelectId, selectedProvince, selectedWard })
// - provinceSelectId / wardSelectId: id của 2 thẻ <select>
// - selectedProvince / selectedWard: tên đã lưu trước đó (dùng khi Edit), có thể để trống

(function () {
    const API_BASE = "https://provinces.open-api.vn/api/v2";

    async function fetchProvinces() {
        const res = await fetch(`${API_BASE}/p/`);
        if (!res.ok) throw new Error("Không tải được danh sách tỉnh/thành.");
        return res.json();
    }

    async function fetchWardsByProvinceCode(provinceCode) {
        const res = await fetch(`${API_BASE}/p/${provinceCode}?depth=2`);
        if (!res.ok) throw new Error("Không tải được danh sách xã/phường.");
        const data = await res.json();
        return data.wards || [];
    }

    function fillSelect(selectEl, items, placeholder, selectedName) {
        selectEl.innerHTML = "";
        const optDefault = document.createElement("option");
        optDefault.value = "";
        optDefault.textContent = placeholder;
        selectEl.appendChild(optDefault);

        items.forEach((item) => {
            const opt = document.createElement("option");
            opt.value = item.name;
            opt.textContent = item.name;
            opt.dataset.code = item.code;
            if (selectedName && item.name === selectedName) {
                opt.selected = true;
            }
            selectEl.appendChild(opt);
        });
    }

    window.initAddressCascade = async function (opts) {
        const provinceSelect = document.getElementById(opts.provinceSelectId);
        const wardSelect = document.getElementById(opts.wardSelectId);
        if (!provinceSelect || !wardSelect) return;

        wardSelect.disabled = true;
        provinceSelect.innerHTML = "<option value=''>Đang tải...</option>";

        let provinces = [];
        try {
            provinces = await fetchProvinces();
        } catch (e) {
            provinceSelect.innerHTML = "<option value=''>Lỗi tải dữ liệu, vui lòng thử lại</option>";
            console.error(e);
            return;
        }

        fillSelect(provinceSelect, provinces, "-- Chọn Tỉnh/Thành phố --", opts.selectedProvince);

        async function loadWardsForCurrentProvince(selectedWardName) {
            const selectedOption = provinceSelect.options[provinceSelect.selectedIndex];
            const code = selectedOption ? selectedOption.dataset.code : null;

            if (!code) {
                wardSelect.innerHTML = "<option value=''>-- Chọn Tỉnh/Thành phố trước --</option>";
                wardSelect.disabled = true;
                return;
            }

            wardSelect.disabled = true;
            wardSelect.innerHTML = "<option value=''>Đang tải...</option>";
            try {
                const wards = await fetchWardsByProvinceCode(code);
                fillSelect(wardSelect, wards, "-- Chọn Xã/Phường --", selectedWardName);
                wardSelect.disabled = false;
            } catch (e) {
                wardSelect.innerHTML = "<option value=''>Lỗi tải dữ liệu, vui lòng thử lại</option>";
                console.error(e);
            }
        }

        provinceSelect.addEventListener("change", () => loadWardsForCurrentProvince(null));

        // Nếu có địa chỉ đã lưu sẵn (trang Edit), tự động load Xã/Phường tương ứng.
        if (opts.selectedProvince) {
            await loadWardsForCurrentProvince(opts.selectedWard);
        } else {
            wardSelect.innerHTML = "<option value=''>-- Chọn Tỉnh/Thành phố trước --</option>";
        }
    };
})();