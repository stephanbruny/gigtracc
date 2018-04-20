const request = require('request-promise');
const modal = require('../modal');
const Entry = require('../model/entry');
require('moment/locale/de');
const moment = require('moment');
const el = require('crel');
const uiUtils = require('../ui-utils');
const url = require('../model/url');
const projectEdit = require('./project-edit');
const projectModel = require('../model/project');

const projectTableView = (project, entries) => {
    const table = el('table', { class: 'project-report' });
    const thead = el('thead');
    const tbody = el('tbody');

    el(thead,
        el('th', 'Datum'),
        el('th', 'Stunden'),
        el('th', 'Ort'),
        el('th', 'Beschreibung')
    );

    const rows =
        entries
        .sort((a, b) => {
            if (a.date < b.date) return -1;
            if (a.date > b.date) return 1;
            return 0;
        })
        .map(entry => {
            return el('tr',
                el('td', moment(entry.date).format('dd DD.MM') ),
                el('td', { class: 'num' }, entry.duration.toFixed(2)),
                el('td', entry.location),
                el('td', entry.description)
            )
        });

    const total = entries.reduce((acc, e) => acc + e.duration, 0);
    const totalPrice = project.payment.pricePerHour * total;
    const tax = totalPrice * 0.19; // TODO: Get tex value
    const totalTaxed = totalPrice + tax;

    const displayPrice = value => `${value.toFixed(2)} ${project.payment.currency}`;

    const tableDivider = el('tr', { class: 'table-divider' }, el('td', { colspan: 3 }, "Rechnung"));

    const tableTotalHours = el('tr',
        el('td', 'Gesamt: '),
        el('td', { class: 'num' }, total.toFixed(2)),
        el('td'),
        el('td')
    );

    const tableSum = el('tr', { class: 'bill' },
        el('td', { colspan: 2 }, 'Stundensatz: ' + displayPrice(project.payment.pricePerHour) + ' x ' + total.toFixed(2)),
        el('td'),
        el('td', { class: 'num' }, displayPrice(totalPrice))
    );

    const tablePriceCalc = el('tr', { class: 'bill' },
        el('td', 'USt. (19%)'),
        el('td'),
        el('td'),
        el('td', { class: 'num' }, displayPrice(tax)),
    );

    const tableTotalWithTax = el('tr', { class: 'bill' },
        el('td', 'Gesamt'),
        el('td'),
        el('td'),
        el('td', { class: 'num' }, displayPrice(totalTaxed))
    );

    el(tbody, rows, tableTotalHours, tableDivider, tableSum, tablePriceCalc, tableTotalWithTax);

    return el(table, thead, tbody);
}

const getWeeks = (date) => {
    const start = moment(date).utc().startOf('month');
    const end = moment(date).utc().endOf('month');

    return moment.duration(end - start).weeks() + 1;
}

module.exports = (parent) => (project, monthNumber) => {
    const weekCount = getWeeks(moment().month(monthNumber));
    const newEntry = Entry(project);

    const page = el('div', { class: 'project' });
    const header = el('div', { class: 'project-header' });
    const projectSummary =  el('div', { id: 'project-summary' });
    const calender = el('div', { id: 'calender' });

    el(page, header, calender);

    const doc = el(parent, page);

    moment.locale('de');

    const projectButtons = el('div', { class: 'btn-list' });
    const projectReportButton = el('button', { class: 'btn hover-shadow' }, el('i', { class: 'fa fa-table fa-2x' }));
    const projectSettingsButton = el('button', { class: 'btn hover-shadow' }, el('i', { class: 'fa fa-cog fa-2x' }));
    el(projectButtons, projectReportButton, projectSettingsButton);

    const loadEntries = month => cb => {

        const currentMonth = moment().month(month);
        const dayCount = currentMonth.daysInMonth();
        uiUtils.clear(header);
        uiUtils.clear(calender);
        uiUtils.clear(projectSummary);
        el(projectSummary, el('h2', project.name));
        const startDate = moment().month(month).format('YYYY-MM-01');
        const endDate = moment().month(month).format('YYYY-MM-' + dayCount.toString());

        el(header, projectSummary, totalHoursContainer, projectButtons);

        request.get(url('api', 'entries', project.id, startDate, endDate))
            .then(JSON.parse)
            .then(cb(month, dayCount))
    }

    const getTotalHours = entries => entries.reduce((acc, e) => acc + e.getData().duration, 0) || 0;

    const totalHoursContainer = el('div', { class: 'total-hours' });
    el(header, totalHoursContainer);


    const renderTotalHours = total => {
        uiUtils.clear(totalHoursContainer);
        el(totalHoursContainer, el('h2', 'Total: ' + total.toString()));
    }

    const renderCalender = (currentMonth, dayCount) => (entries) => {

        projectReportButton.addEventListener('click', () => {
            modal(projectTableView(project, entries)).show();
        });

        projectSettingsButton.addEventListener('click', () => {
            const container = el('div');
            const updateModal = modal(container, project.name);
            updateModal.show();
            const model = projectModel(project);
            return projectEdit(container, project)
                .then(model.update)
                .catch(err => { console.error(err) })
                .then(() => updateModal.close());
        });

        renderTotalHours(getTotalHours(entries.map(newEntry)));
        Array.from({length: dayCount}, (v, i) => i).forEach((_, i) => {
            const today = moment();
            const itemDayOfMonth = i + 1;
            const itemDate = moment().month(currentMonth).date(itemDayOfMonth);
            const itemWeek = moment(itemDate).week();
            const isWeekEnd = moment(itemDate).weekday() >= 5;

            const itemEntries = entries.filter(e => moment(itemDate).isSame(moment(e.date), 'day') ).map(newEntry);
            const hasEntries = !!itemEntries.length;

            if (!hasEntries) itemEntries.push(newEntry({}));

            const d = el('p', moment(itemDate).format('dd DD'));
            const dayCol = el('div', { class: `day-of-month ${isWeekEnd ? 'weekend' : ''}` }, d);
            const totalHours = getTotalHours(itemEntries);
            const totalHoursItem = el('div', { class: 'date-entry' }, totalHours.toString());
            if (totalHours && totalHours > 0) dayCol.classList.add('work-done');
            el(dayCol, totalHoursItem);
            if (moment().month() === currentMonth && today.date() === itemDayOfMonth) {
                dayCol.classList.add('today');
            }
            // itemEntries.forEach(e => el(dayCol, el('div', { class: 'date-entry' }, e.duration.toString())));
            el(calender, dayCol);
            dayCol.addEventListener('click', () => {
                const modalHeader = el('span', moment(itemDate).format('dddd DD.MM.YYYY'));
                const entryModal = modal(el('div'), modalHeader);
                const currentEntries = itemEntries.map(entry => {
                    const e = entry.getData();
                    const container = el('div', { class: 'entry' });
                    const containerHeader = el('div', { class: 'entry-header' });
                    const durationInput = el('input', { type: 'number', value: e.duration || 0.0 }, e.duration );
                    const locationInput = el('input', { type: 'text', value: e.location || ""});
                    el(
                        containerHeader,
                        el('label', 'Ort:', locationInput),
                        el('label', 'Stunden:', durationInput)
                    );
                    el(container, containerHeader);
                    const descriptionInput = el('textarea', e.description);
                    el(container, el('label', 'Beschreibung:'), descriptionInput);
                    const saveButton = el('button', { class: 'btn btn-bg btn-save' }, el('i', { class: 'fas fa-save fa-lg white' }));
                    const removeButton = el('button', { class: 'btn btn-bg btn-red' }, el('i', { class: 'fas fa-trash fa-lg white' }));
                    const entryButtons = el('div', { class: 'entry-buttons' });
                    saveButton.addEventListener('click', () => {
                        if (hasEntries) {
                            return Promise.all([
                                entry.modifyDescription(descriptionInput.value),
                                entry.modifyDuration(durationInput.value),
                                entry.modifyLocation(locationInput.value)
                            ])
                            .then(() => {
                                entryModal.close();
                                totalHoursItem.innerHTML = entry.getData().duration;
                            });
                        }
                        return entry.create(itemDate, locationInput.value, parseFloat(durationInput.value), descriptionInput.value)
                            .then(() => {
                                uiUtils.clear(calender);
                                loadEntries(currentMonth)(renderCalender);
                                entryModal.close();
                            });
                    });
                    removeButton.addEventListener('click', () => {
                        entry.delete()
                        .then(() => {
                            uiUtils.clear(calender);
                            loadEntries(currentMonth)(renderCalender);
                            entryModal.close();
                        })
                    })
                    el(entryButtons, removeButton, saveButton);
                    el(container, entryButtons);
                    return container;
                });
                entryModal.setContent(currentEntries);
                entryModal.show();
            });

        });
    }
    loadEntries(monthNumber)(renderCalender);
}