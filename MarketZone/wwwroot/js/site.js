(function () {
    const dropdown = document.querySelector('.user-dropdown');
    if (!dropdown) return;

    const toggleBtn = dropdown.querySelector('.user-dropdown-toggle');
    const menu = dropdown.querySelector('.user-dropdown-menu');
    if (!toggleBtn || !menu) return;

    function setOpen(isOpen) {
        dropdown.classList.toggle('open', isOpen);
        toggleBtn.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
    }

    toggleBtn.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        setOpen(!dropdown.classList.contains('open'));
    });

    document.addEventListener('click', function (e) {
        if (!dropdown.contains(e.target)) {
            setOpen(false);
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            setOpen(false);
        }
    });
})();
