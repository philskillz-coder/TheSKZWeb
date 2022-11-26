
var mfaInput = document.getElementById("mfa-input");

async function mfaPost() {
    await advancedPost(
        "",
        {},
        {
            "mfa": true,
            "mfaCode": mfaInput.value
        },
        true
    );
    mfaInput.value = "";
}
