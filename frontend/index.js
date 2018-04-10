(function() {
    const request = require('request-promise');
    const modal = require('./modal');
    const Entry = require('./entry');
    require('moment/locale/de');
    const moment = require('moment');
    const el = require('crel');
    const uiUtils = require('./ui-utils');

    const test = el('p', { class: 'foo' }, 'Test');

    const page = el('div', { id: 'page' });

    const header = el('div', { id: 'header' });
    const projectSummary =  el('div', { id: 'project-summary' }, el('h2', 'Project: Test | ' + moment().format('MMMM YYYY')));
    
    const calender = el('div', { id: 'calender' });

    el(page, header, calender);

    const doc = el(document.body, page);

    moment.locale('de');

    const loadEntries = month => cb => {

        const prevMonthButton = el('button', { class: 'btn hover-shadow' }, el('i', { class: 'white fas fa-chevron-left fa-2x' }));
        const nextMonthButton = el('button', { class: 'btn hover-shadow' }, el('i', { class: 'white fas fa-chevron-right fa-2x' }));

        const currentMonth = moment().month(month);
        const dayCount = currentMonth.daysInMonth();
        uiUtils.clear(header);
        uiUtils.clear(calender);
        uiUtils.clear(projectSummary);
        el(projectSummary, el('h2', 'Project: Test | ' + moment().month(month).format('MMMM YYYY')));
        const startDate = moment().month(month).format('YYYY-MM-01');
        const endDate = moment().month(month).format('YYYY-MM-' + dayCount.toString());

        prevMonthButton.addEventListener('click', () => loadEntries(month - 1)(renderCalender));
        nextMonthButton.addEventListener('click', () => loadEntries(month + 1)(renderCalender));
        el(header, prevMonthButton, projectSummary, totalHoursContainer, projectSettingsButton, nextMonthButton);

        request.get(`http://localhost:8080/api/entries/${startDate}/${endDate}`)
            .then(JSON.parse)
            .then(cb(month, dayCount))
    }

    const getTotalHours = entries => entries.reduce((acc, e) => acc + e.getData().duration, 0) || 0;

    const totalHoursContainer = el('div', { class: 'total-hours' });
    el(header, totalHoursContainer);

    const projectButtons = el('div', { class: 'btn-list' });
    const projectSettingsButton = el('button', { class: 'btn hover-shadow' }, el('i', { class: 'white fa fa-cog fa-2x' }));
    el(projectButtons, projectSettingsButton);



    const renderTotalHours = total => {
        uiUtils.clear(totalHoursContainer);
        el(totalHoursContainer, el('h2', 'Total: ' + total.toString()));
    }

    const renderCalender = (currentMonth, dayCount) => (entries) => {
        renderTotalHours(getTotalHours(entries.map(Entry)));
        Array.from({length: dayCount - 1}, (v, i) => i).forEach((_, i) => {
            const today = moment();
            const itemDayOfMonth = i + 1;
            const itemDate = moment().month(currentMonth).date(itemDayOfMonth);

            const itemEntries = entries.filter(e => moment(itemDate).isSame(moment(e.date), 'day') ).map(Entry);
            const hasEntries = !!itemEntries.length;

            if (!hasEntries) itemEntries.push(Entry({}));

            const d = el('p', moment(itemDate).format('dd DD'));
            const dayCol = el('div', { class: 'day-of-month'}, d);
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
                    const durationInput = el('input', { type: 'number', value: e.duration }, e.duration );
                    const locationInput = el('input', { type: 'text', value: e.location});
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

    loadEntries(moment().month())(renderCalender);
})();