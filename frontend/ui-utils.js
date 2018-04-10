const el = require('crel');

module.exports = {
    clear: element => {
        while (element.firstChild) {
            element.removeChild(element.firstChild);
        }
    }
}