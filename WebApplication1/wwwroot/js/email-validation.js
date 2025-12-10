document.addEventListener("DOMContentLoaded", function () {
    const emailInput = document.querySelector("#Email");
    const errorSpan = document.querySelector('[data-valmsg-for="Email"]');

    if (!emailInput) return;

    emailInput.addEventListener("input", function () {
        const email = emailInput.value;

        // RESET ERROR
        errorSpan.textContent = "";
        errorSpan.classList.remove("text-danger");

        // Validation Rules
        if (email.includes(" ")) {
            showError("Spaces are not allowed in email.");
        }
        else if ((email.match(/@/g) || []).length !== 1) {
            showError("Email must contain exactly one '@' symbol.");
        }
        else if (/[A-Z]/.test(email)) {
            showError("Capital letters are not allowed.");
        }
        else if (!email.endsWith("@gmail.com")) {
            showError("Email must end with @gmail.com.");
        }
        else if (!/^[a-z][a-z0-9._%+-]*@gmail\.com$/.test(email)) {
            showError("Invalid email format.");
        }

        function showError(msg) {
            errorSpan.textContent = msg;
            errorSpan.classList.add("text-danger");
        }
    });
});
