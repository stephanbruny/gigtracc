const el = require('crel');
const Nanocal = require('nanocal');
const modal = require('./modal');
const moment = require('moment');

module.exports = ({
    inputGroup: (input, label) => el('div', { class: 'input-group' }, el('label', label), input),
    calendarInput: input => {
        input.addEventListener('click', (ev) => {
            ev.preventDefault();
            const pickContainer = el('div');
            const pickModal = modal(pickContainer, null, { class: 'modal modal-borderless' }, true, true);
            const calendar = new Nanocal({ target: pickContainer });
            calendar.on('selectedDay', date =>  {
                input.value = moment(Object.assign(date, { month: date.month - 1 })).format('YYYY-MM-DD');
                pickModal.close();
            });
            pickModal.show();
        });
    }
})