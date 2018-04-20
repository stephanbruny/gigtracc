const el = require('crel');
const Nanocal = require('nanocal');
const modal = require('./modal');

module.exports = {
    clear: element => {
        while (element.firstChild) {
            element.removeChild(element.firstChild);
        }
    }
}