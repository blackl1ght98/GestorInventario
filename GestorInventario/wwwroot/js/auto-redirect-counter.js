document.addEventListener('DOMContentLoaded', function () {
    var counter = 5;
    var interval = setInterval(function () {
        counter--;
        if (counter >= 0) {
            document.getElementById("counter").textContent = counter;
        }
        if (counter === 0) {
            window.location.href = "/Productos";
            clearInterval(interval);
        }
    }, 1000);
});