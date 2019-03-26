define("seositemaps/Editor", [
        "dojo/_base/declare",
        "dijit/_Widget",
        "dijit/_TemplatedMixin",
        "dijit/_WidgetsInTemplateMixin",
        "dojox/xml/DomParser",
        "dojo/text!./templates/SeoSitemapProperty.html",
        "epi-cms/contentediting/editors/SelectionEditor",
        "epi/shell/widget/CheckBox"
    ],
    function (
        declare,
        _Widget,
        _TempateMixing,
        _WidgetsInTemplateMixin,
        domParser,
        template,
        SelectionEditor
    ) {

        return declare(
            [_Widget, _TempateMixing, _WidgetsInTemplateMixin],
            {
                templateString: template,
                postCreate: function () {
                    this.inherited(arguments);
                    this._frequencySelectEditor = new SelectionEditor({ selections: this._getfrequencySelections(), parent: this });
                    this._frequencySelectEditor.on("change", this._frequencyOnChange);
                    this._frequencySelectEditor.placeAt(this.frequencySelect);
                    this._prioritySelectEditor = new SelectionEditor({ selections: this._getPrioritySelections(), parent: this });
                    this._prioritySelectEditor.on("change", this._priorityOnChange);
                    this._prioritySelectEditor.placeAt(this.prioritySelect);
                },

                _frequencySelectEditor: null,

                _getfrequencySelections: function () {
                    return [
                        { value: "always", text: "Always" },
                        { value: "hourly", text: "Hourly" },
                        { value: "daily", text: "Daily" },
                        { value: "weekly", text: "Weekly" },
                        { value: "monthly", text: "Monthly" },
                        { value: "yearly", text: "Yearly" },
                        { value: "never", text: "Never" }
                    ];
                },

                _getPrioritySelections: function () {
                    return [
                        { value: "0.0", text: "Low(0.0)" },
                        { value: "0.25", text: "Low (0.25)" },
                        { value: "0.5", text: "Medium (0.5)" },
                        { value: "0.75", text: "Medium-High (0.75)" },
                        { value: "1.0", text: "High (1.0)" }
                    ];
                },

                _prioritySelectEditor: null,

                _priority: "0.5",
                _frequency: "weekly",
                _enabled: true,

                _setValueAttr: function (value) {
                    if (value) {
                        var jsDom = domParser.parse(value);

                        var enabledNode = jsDom.byName("enabled")[0];
                        if (enabledNode.childNodes.length) {
                            this._enabled = enabledNode.childNodes[0].nodeValue;
                        }

                        var frequencyNode = jsDom.byName("changefreq")[0];
                        if (frequencyNode.childNodes.length) {
                            this._frequency = frequencyNode.childNodes[0].nodeValue;
                        }

                        var priorityNode = jsDom.byName("priority")[0];
                        if (priorityNode.childNodes.length) {
                            this._priority = priorityNode.childNodes[0].nodeValue;
                        }
                    }
                    this.enabledCheckbox.set("value", this._enabled);
                    this._frequencySelectEditor.set("value", this._frequency);
                    this._prioritySelectEditor.set("value", this._priority);
                    this._set('value', value);
                },

                isValid: function () {
                    return true;
                },

                _setXml: function () {

                    this._set('value', "<SEOSitemaps>" +
                        "<enabled>" + this._enabled + "</enabled>" +
                        "<changefreq>" + this._frequency + "</changefreq>" +
                        "<priority>" + this._priority + "</priority>" +
                        "</SEOSitemaps>");
                    this.onChange(this.value);
                },

                _enabledOnChange: function (value) {
                    this._enabled = value;
                    this._setXml();
                },

                _frequencyOnChange: function (value) {
                    this.parent._frequency = value;
                    this.parent._setXml();
                },

                _priorityOnChange: function (value) {
                    this.parent._priority = value;
                    this.parent._setXml();
                }
            });
    }
);