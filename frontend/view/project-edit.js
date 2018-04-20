const el = require('crel');
const moment = require('moment');
const widgets = require('../widgets');
const modal = require('../modal');

module.exports = (parent, projectData) => {
    const form = el('form', { class: 'create-project' });
    const nameInput = el('input');
    const descriptionInput = el('textarea');
    const clientNameInput = el('input');
    const clientAddressInput = el('input');
    const startDateInput = el('input', { readonly: true });
    const endDateInput = el('input', { readonly: true });
    const priceInput = el('input', { type: 'number' });
    const taxInput = el('input', { type: 'number', value: 19 }, 19);

    if (projectData) {
        nameInput.value = projectData.name;
        descriptionInput.value = projectData.description;
        clientNameInput.value = projectData.client.name;
        clientAddressInput.value = projectData.client.address;
        startDateInput.value = moment(projectData.startDate).format('YYYY-MM-DD');
        endDateInput.value = moment(projectData.endDate).format('YYYY-MM-DD');
        priceInput.value = projectData.payment.pricePerHour;
        taxInput.value = projectData.payment.tax;
    }

    widgets.calendarInput(startDateInput);
    widgets.calendarInput(endDateInput);

    el(
        form,
        widgets.inputGroup(nameInput, 'Name'),
        el('div', {class: 'row'},
            widgets.inputGroup(clientNameInput, 'Kunde'),
            widgets.inputGroup(clientAddressInput, 'Kundenadresse')
        ),
        widgets.inputGroup(descriptionInput, 'Projektbeschreibung'),
        el('div', {class: 'row'},
            widgets.inputGroup(startDateInput, 'Startdatum'),
            widgets.inputGroup(endDateInput, 'Enddatum')
        ),
        el('div', { class: 'row' },
            widgets.inputGroup(priceInput, 'Stundensatz'),
            widgets.inputGroup(taxInput, 'USt. in %'),
        )
    );
    el(parent, form);

    const getValues = () => ({
        name : nameInput.value,
        description : descriptionInput.value,
        client : {
            name : clientNameInput.value,
            address : clientAddressInput.value
        },
        startDate : startDateInput.value,
        endDate: endDateInput.value,
        pricePerHour: parseFloat(priceInput.value),
        tax: parseFloat(taxInput.value)
    });

    const okButton = el('button', { class: 'btn btn-bg' }, 'OK');
    el(form, okButton)
    return new Promise((resolve, reject) => {
        okButton.addEventListener('click', ev => {
            ev.preventDefault();
            return resolve(getValues())
        })
    });
}