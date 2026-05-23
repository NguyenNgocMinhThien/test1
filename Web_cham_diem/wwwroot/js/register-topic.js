// Register Topic Page - Interactivity

document.addEventListener('DOMContentLoaded', function () {
    const typeIndividual = document.getElementById('typeIndividual');
    const typeTeam = document.getElementById('typeTeam');
    const teamInfoSection = document.getElementById('teamInfoSection');
    const competitionSelect = document.getElementById('competitionSelect');
    const competitionInfo = document.getElementById('competitionInfo');

    // Toggle team info section based on registration type
    function toggleTeamSection() {
        if (typeTeam.checked) {
            teamInfoSection.classList.remove('d-none');
            document.querySelector('[asp-for="TeamName"]').setAttribute('required', 'required');
        } else {
            teamInfoSection.classList.add('d-none');
            document.querySelector('[asp-for="TeamName"]').removeAttribute('required');
        }
    }

    typeIndividual.addEventListener('change', toggleTeamSection);
    typeTeam.addEventListener('change', toggleTeamSection);

    // Load competitions and show competition info
    competitionSelect.addEventListener('change', async function () {
        if (!this.value) {
            competitionInfo.classList.add('d-none');
            document.getElementById('summaryContent').innerHTML = `
                <i class="bi bi-clipboard-list fs-3 d-block mb-2"></i>
                <p class="small">Ch?n cu?c thi ð? xem thông tin</p>
            `;
            return;
        }

        // Mock data - thay b?ng API call th?c t?
        try {
            const response = await fetch(`/RegisterTopic/GetCompetitions`);
            const competitions = await response.json();

            const selected = competitions.find(c => c.competitionId == this.value);
            if (selected) {
                displayCompetitionInfo(selected);
            }
        } catch (error) {
            console.error('Error loading competitions:', error);
        }
    });

    function displayCompetitionInfo(competition) {
        // Display in main area
        document.getElementById('competitionCategory').textContent = competition.category || 'N/A';
        document.getElementById('competitionType').textContent = competition.isTeamBased ? 'Ð?i thi' : 'Cá nhân';
        document.getElementById('registrationDeadline').textContent = formatDate(competition.registrationDeadline);
        document.getElementById('submissionDeadline').textContent = formatDate(competition.submissionDeadline);
        competitionInfo.classList.remove('d-none');

        // Display in summary
        document.getElementById('summaryContent').innerHTML = `
            <div class="text-start">
                <h6 class="fw-bold text-dark mb-3">${competition.competitionName}</h6>
                <div class="mb-2">
                    <small class="text-muted d-block">L?nh v?c</small>
                    <span class="small fw-semibold">${competition.category || 'N/A'}</span>
                </div>
                <div class="mb-2">
                    <small class="text-muted d-block">Lo?i cu?c thi</small>
                    <span class="small fw-semibold">${competition.isTeamBased ? '???????? Ð?i thi' : '?? Cá nhân'}</span>
                </div>
                <div class="mb-2">
                    <small class="text-muted d-block">H?n ðãng k?</small>
                    <span class="small fw-semibold text-primary">${formatDate(competition.registrationDeadline)}</span>
                </div>
                <div>
                    <small class="text-muted d-block">H?n n?p bài</small>
                    <span class="small fw-semibold text-primary">${formatDate(competition.submissionDeadline)}</span>
                </div>
            </div>
        `;
    }

    function formatDate(dateString) {
        if (!dateString) return 'N/A';
        const options = { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' };
        return new Date(dateString).toLocaleDateString('vi-VN', options);
    }

    // Form validation with Bootstrap
    const form = document.querySelector('.register-topic-form');
    if (form) {
        form.addEventListener('submit', function (event) {
            if (!form.checkValidity() === false) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        });
    }

    // Initialize on page load if there was a previous selection
    if (competitionSelect.value) {
        const event = new Event('change');
        competitionSelect.dispatchEvent(event);
    }
});
