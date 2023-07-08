const tooltips
    = [];
const tooltipOptions = {
    trigger: "hover",
    delay: {
        show: 750,
        hide: 0
    }
};
function enableTooltips() {
    function enable() {
        var elements = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        elements.forEach((e) => {
            if (tooltips.indexOf(e) === -1) {
                new bootstrap.Tooltip(e, tooltipOptions);
                tooltips.push(e);
            }
        });
    }
    enable();
    setInterval(enable, 1000);
}

setTimeout(enableTooltips, 4000);