const enabled = [];
function enableTooltips() {
    const tooltipOptions = {
        trigger: "hover",
        delay: {
            show: 750,
            hide: 0
        }
    };

    function enable() {
        var elements = document.querySelectorAll('[data-bs-toggle="tooltip"]');

        [].slice.call(elements).forEach((e) => {
            if (enabled.indexOf(e) === -1) {
                new bootstrap.Tooltip(e, tooltipOptions);
                enabled.push(e);
            }
        });
    }
    enable();
    setInterval(enable, 1000);
}

setTimeout(enableTooltips, 4000);