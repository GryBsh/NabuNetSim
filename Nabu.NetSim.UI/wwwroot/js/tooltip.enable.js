const tooltips = [];
const tooltipElements = [];
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
        var missing = tooltipElements.filter((e) => elements.indexOf(e) === -1);
        missing.forEach((e) => {
            try {
                var tip = tooltips.filter((t) => t._element === e)[0];
                tip.dispose();
            } catch (e) { }
        });
        elements.forEach((e) => {
            if (tooltipElements.indexOf(e) === -1) {
                var tip = new bootstrap.Tooltip(e, tooltipOptions);
                tooltips.push(tip);
                tooltipElements.push(e);
            }
        });
    }
    enable();
    setInterval(enable, 10);
}

setTimeout(enableTooltips, 4000);