(
    function __() {

        String.prototype.trim = function () {
            return this.replace(/^\s+|\s+$/, '');
        };

        String.isNullOrEmpty = function (val) {
            return !val || !val.length || !val.trim().length;
        };

        String.isInteger = function (val) {
            return /^\d+$/.test(val);
        };

        Date.prototype.toTime = function () {
            var hh = this.getHours();
            var mm = this.getMinutes();
            var ss = this.getSeconds();
            var am = hh < 12;

            if (hh === 0) hh = 12;
            if (hh > 12) hh -= 12;

            return [
                hh.toString(),
                mm < 10 ? ':0' : ':',
                mm.toString(),
                ss < 10 ? ':0' : ':',
                ss.toString(),
                am ? ' AM' : ' PM'
            ].join('');
        };


        var __global = function () { };

        __global.prototype = {
            endpoint: initPara.endpoint,
            apiVersion: initPara.apiVersion,
            user: initPara.user,
            xmlFactories: [
                function () { return new XMLHttpRequest(); },
                function () { return new ActiveXObject("Msxml2.XMLHTTP"); },
                function () { return new ActiveXObject("Msxml3.XMLHTTP"); },
                function () { return new ActiveXObject("Microsoft.XMLHTTP"); }
            ],
            categoriesLoaded: false,
            editMode: null,
            editID: null,
            firstEdit: false,
            generalAdminIdx: -1,
            errors: [],
            changes: [],
            broadcastID: null,
            accessToken: initPara.accessToken,
            refreshToken: null,
            __sendRequest: function (url, method, data, successCallback, errorCallback, auth, done) {
                var ajax = TimeKeep.getAjax();
                if (!ajax) return;
                ajax.open(method, url, successCallback !== null);
                ajax.setRequestHeader('Accept', 'application/json');
                ajax.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
                if (auth)
                    ajax.setRequestHeader('Authorization', 'Bearer ' + TimeKeep.accessToken);
                ajax.setRequestHeader('Content-Type', 'application/json;charset=UTF-8');
                ajax.eCallback = errorCallback;
                ajax.sCallback = successCallback;
                ajax.dCallback = done;
                ajax.onreadystatechange = function () {
                    if (this.readyState !== 4) return;
                    if (this.status > 399 && this.eCallback) {
                        var data = null;
                        try {
                            data = JSON.parse(this.responseText);
                        }
                        catch (e) {
                            // TODO: Do domething
                        }

                        this.eCallback({
                            ajax: this,
                            data: data,
                            status: this.status
                        });
                    }
                    else if (this.sCallback) {
                        var data2 = null;
                        try {
                            data2 = JSON.parse(this.responseText).Result;
                        }
                        catch (e) {
                            // TODO: Handle
                        }
                        this.sCallback({
                            ajax: this,
                            data: data2,
                            status: this.status
                        });
                    }
                    if (this.dCallback) {
                        this.dCallback();
                    }
                };
                ajax.send(data ? JSON.stringify(data) : '');

                if (!successCallback)
                    return {
                        ajax: ajax,
                        data: JSON.parse(this.responseText),
                        status: ajax.status
                    };
            },
            lastDay: new Date().getDate(),
            clock: function () {
                var replace = function () {

                    var now = new Date();
                    var hh = now.getHours();
                    var mm = now.getMinutes();
                    var ss = now.getSeconds();
                    var am = hh < 12;

                    if (hh > 12)
                        hh -= 12;
                    else if (hh == 0)
                        hh = 12;

                    mm = (mm < 10 ? '0' : '') + mm.toString();
                    ss = (ss < 10 ? '0' : '') + ss.toString();

                    var str = [hh, ':', mm, ':', ss, am ? ' AM' : ' PM'];

                    document.getElementById('currentTime').innerHTML = str.join('');
                    setTimeout(replace, 1000);

                    if (now.getDate() !== TimeKeep.lastDay) {
                        TimeKeep.endOfDay();
                        TimeKeep.lastDay = now.getDate();
                    }
                };

                replace();
            },
            remoteLock: 0,
            localLock: 0,
            getUTCDateRange: function () {
                var dt = new Date();
                var dateStart = new Date(dt.getFullYear(), dt.getMonth(), dt.getDate(), 0, 0, 0, 0);
                var dateEnd = new Date(dt.getFullYear(), dt.getMonth(), dt.getDate() + 1, 0, 0, 0, 0);
                return {
                    Start: dateStart.toISOString(),
                    End: dateEnd.toISOString()
                };
            },
            getAjax: function () {
                for (var i = 0; i < TimeKeep.xmlFactories.length; i++) {
                    try {
                        return TimeKeep.xmlFactories[i]();
                    }
                    catch (e) {
                        continue;
                    }
                }
            },
            getAPIURL: function (relativePath) {
                return TimeKeep.endpoint + relativePath + "?api-version=" + TimeKeep.apiVersion;
            },
            sendRequest: function (url, method, data, successCallback, errorCallback, done) {
                return TimeKeep.__sendRequest(TimeKeep.getAPIURL(url), method, data, successCallback, errorCallback, true, done);
            },
            sendLocalRequest: function (url, method, data, successCallback, errorCallback, done) {
                return TimeKeep.__sendRequest(url, method, data, successCallback, errorCallback, false, done);
            },
            btnStartYourDate: function () {
                var data = {
                    ID: null,
                    User: TimeKeep.user
                };

                TimeKeep.sendRequest('/timekeepentries', 'POST', data,
                    function success(result) {
                        if (!result || !result.data) {
                            return; // TODO; handle
                        }
                        if (result.data.Modified) {
                            var modchange = { changeType: 'Modified', data: result.data.Modified };
                            TimeKeep.broadcastChange(modchange);
                            TimeKeep.changes.push(modchange);
                        }
                        var newchange = { changeType: 'Added', data: result.data.New };
                        TimeKeep.changes.push(newchange);
                        TimeKeep.broadcastChange(newchange);
                        TimeKeep.editMode = null;
                        TimeKeep.megaUpdate();
                    },
                    function error(result) {
                        // TODO: Handle
                    }
                );
            },
            init: function () {

                TimeKeep.clock();

                TimeKeep.sendRequest('/timekeepentries/user/' + TimeKeep.user, 'POST', TimeKeep.getUTCDateRange(),
                    function success(result) {
                        if (result && result.data) {
                            document.getElementById(result.data.length ? 'entries' : 'noentries').style.display = '';
                            var loading = document.getElementById('mainloading');
                            loading.parentElement.removeChild(loading);
                            loading = null;
                            for (var i = 0; i < result.data.length; i++) {
                                TimeKeep.changes.push({ changeType: 'Added', data: result.data[i] });
                            }
                            TimeKeep.processChanges();
                        }
                        else {
                            // TODO: Critical error, handle
                        }
                    },
                    function error(result) {
                        // TODO: Critical error
                    }
                );

                TimeKeep.sendRequest('/categories', 'GET', null,
                    function success(result) {

                        if (result && result.data && result.data.length) {
                            var sbSC = [];
                            var sbNSC = [];
                            var sbOut = [];


                            for (var i = 0; i < result.data.length; i++) {
                                var cat = result.data[i];

                                if (cat.IsOut) {
                                    sbOut.push('<option value="');
                                    sbOut.push(cat.ID.toString());
                                    sbOut.push('">');
                                    sbOut.push(cat.Description);
                                    sbOut.push('</option>');
                                }
                                else if (cat.IsScorecard) {
                                    sbSC.push('<option value="');
                                    sbSC.push(cat.ID.toString());
                                    sbSC.push('">');
                                    sbSC.push(cat.Description);
                                    sbSC.push('</option>');
                                }
                                else {
                                    sbNSC.push('<option value="');
                                    sbNSC.push(cat.ID.toString());
                                    sbNSC.push('">');
                                    sbNSC.push(cat.Description);
                                    sbNSC.push('</option>');
                                }
                            }

                            document.getElementById('CategoryScorecard').innerHTML = sbSC.join('');
                            document.getElementById('CategoryNonscorecard').innerHTML = sbNSC.join('');
                            document.getElementById('CategoryOut').innerHTML = sbOut.join('');

                            var nonscEls = document.getElementById('CategoryNonscorecard').parentElement.options;

                            for (var j = 0; j < nonscEls.length; j++) {
                                if (nonscEls[j].innerHTML === 'General Administration')
                                {
                                    TimeKeep.generalAdminIdx = j;
                                    break;
                                }
                            }

                            document.getElementById('CategoryNonscorecard').parentElement.options[TimeKeep.generalAdminIdx].selected = true;

                            TimeKeep.categoriesLoaded = true;
                            TimeKeep.megaUpdate();
                        }
                        else {
                            // TODO: Critical error, handle
                        }
                    },
                    function error(result) {
                        // TODO: Handle
                    });

                TimeKeep.sendLocalRequest('/broadcast/register', 'POST', { User: TimeKeep.user },
                    function sCallback(result) {
                        if (result && result.data) {
                            TimeKeep.broadcastID = result.data.ID;
                            TimeKeep.startBroadcastListener();
                        }
                        else {
                            // TODO: Handle
                        }
                    },
                    function eCallback(result) {
                        // TODO: Handle
                    }
                );
            },
            broadcastChange: function (change) {
                if (TimeKeep.localLock === 0) {
                    if (++TimeKeep.localLock >= 2) {
                        --TimeKeep.localLock;
                        return;
                    }
                }
                else
                    return;
                TimeKeep.sendLocalRequest('/broadcast/broadcast', 'POST', { ID: TimeKeep.broadcastID, Change: change },
                    function sCallback(result) {
                    },
                    function eCallback(result) {
                        // TODO: Handle
                    },
                    function dCallback() {
                        TimeKeep.localLock = 0;
                    }
                );
            },
            startBroadcastListener: function () {
                var task = function __() {

                    if (TimeKeep.localLock === 0) {
                        if (++TimeKeep.localLock >= 2) {
                            --TimeKeep.localLock;
                            return;
                        }
                    }

                    TimeKeep.sendLocalRequest('/broadcast/changes', 'POST', { ID: TimeKeep.broadcastID },
                        function sCallback(result) {
                            if (result) {
                                var changes = result.data;
                                if (changes && changes.length) {
                                    for (var i = 0; i < changes.length; i++) {
                                        TimeKeep.changes.push(changes[i]);
                                    }
                                    TimeKeep.processChanges();
                                }
                            }
                            else {
                                // TODO: Handle
                            }

                            setTimeout(task, 5000);
                        },
                        function eCallback(result) {
                            // TODO: Handle

                            setTimeout(task, 5000);
                        },
                        function dCallback() {
                            TimeKeep.localLock = 0;
                        }
                    );
                };

                var dereg = function (e) {

                    if (TimeKeep.localLock === 0) {
                        if (++TimeKeep.localLock >= 2) {
                            --TimeKeep.localLock;
                            return;
                        }
                    }



                    TimeKeep.sendLocalRequest('/broadcast/deregister', 'POST', { ID: TimeKeep.broadcastID },
                        function sCallback(result) {
                            // Nothing really, we just want it async
                        },
                        function eCallback(result) {
                            // TODO: Handle
                        },
                        function dCallback() {
                            TimeKeep.localLock = 0;
                        }
                    );
                };

                window.onbeforeunload = dereg;
                setTimeout(task, 3000);
            },
            btnCancelClick: function () {
                document.getElementById('StartTimeHH').selectedIndex = 0;
                document.getElementById('StartTimeMM').selectedIndex = 0;
                document.getElementById('StartTimeSS').selectedIndex = 0;
                document.getElementById('StartTimeAM').selectedIndex = 0;
                document.getElementById('EndTimeHH').selectedIndex = 0;
                document.getElementById('EndTimeMM').selectedIndex = 0;
                document.getElementById('EndTimeSS').selectedIndex = 0;
                document.getElementById('EndTimeAM').selectedIndex = 0;
                document.getElementById('EndTimeAM').selectedIndex = 0;
                document.getElementById('Category').selectedIndex = 0;
                document.getElementById('CaseNumber').value = '';
                TimeKeep.editMode = null;
                TimeKeep.megaUpdate();
            },
            btnSaveClick: function () {

                // Fix: This operation apparently has "concurrency" issues, so decided to change up the locking mechanism
                if (TimeKeep.remoteLock === 0) {
                    if (++TimeKeep.remoteLock >= 2) {
                        --TimeKeep.remoteLock;
                        return;
                    }
                }
                else
                    return;

                document.getElementById('btnSave').innerHTML = 'Saving...';
                document.getElementById('btnSave').disabled = true;
                document.getElementById('btnCancel').disabled = true;


                var errors = [];

                var startTimeHH = parseInt(document.getElementById('StartTimeHH').value, 10);
                var startTimeMM = parseInt(document.getElementById('StartTimeMM').value, 10);
                var startTimeSS = parseInt(document.getElementById('StartTimeSS').value, 10);
                var startTimeAM = document.getElementById('StartTimeAM').value === 'AM';

                if (startTimeHH === 12 && startTimeAM)
                    startTimeHH = 0;
                else if (!startTimeAM && startTimeHH < 12)
                    startTimeHH += 12;


                var endTimeHH = parseInt(document.getElementById('EndTimeHH').value, 10);
                var endTimeMM = parseInt(document.getElementById('EndTimeMM').value, 10);
                var endTimeSS = parseInt(document.getElementById('EndTimeSS').value, 10);
                var endTimeAM = document.getElementById('EndTimeAM').value === 'AM';

                if (endTimeHH === 12 && endTimeAM)
                    endTimeHH = 0;
                else if (!endTimeAM && endTimeHH < 12)
                    endTimeHH += 12;

                var now = new Date();

                var startTime = new Date(now.getFullYear(), now.getMonth(), now.getDate(), startTimeHH, startTimeMM, startTimeSS, 0);
                var endTime = new Date(now.getFullYear(), now.getMonth(), now.getDate(), endTimeHH, endTimeMM, endTimeSS, 0);
                var catEl = document.getElementById('Category');
                var isScorecard = catEl.options[document.getElementById('Category').selectedIndex].parentNode.id === 'CategoryScorecard';
                var isOut = catEl.options[document.getElementById('Category').selectedIndex].parentNode.id === 'CategoryOut';
                var catDescription = catEl.options[document.getElementById('Category').selectedIndex].innerHTML;

                if (TimeKeep.editMode === 'E' && endTime <= startTime)
                    errors.push('The end time must take place after the start time');

                if ((TimeKeep.editMode === 'A' || TimeKeep.editMode === 'E') && isScorecard) {
                    if (String.isNullOrEmpty(document.getElementById('CaseNumber').value))
                        errors.push('The case number is required if the labor is a scorecard labor');
                    else if (!String.isInteger(document.getElementById('CaseNumber').value.trim()))
                        errors.push('The case number must be an integer');
                }

                if (errors.length) {
                    TimeKeep.errors = errors;
                    TimeKeep.remoteLock = 0;
                    TimeKeep.megaUpdate();
                }
                else {
                    TimeKeep.errors = [];
                    TimeKeep.megaUpdate();

                    var data = {
                        ID: TimeKeep.editID,
                        User: TimeKeep.user,
                        Category: {
                            ID: document.getElementById('Category').value,
                            Description: catDescription,
                            IsScorecard: isScorecard,
                            IsOut: isOut
                        },
                        CaseNumber: document.getElementById('CaseNumber').value.trim(),
                        StartTime: startTime.toISOString(),
                        EndTime: endTime.toISOString()
                    };

                    var done = function () {
                        setTimeout(function __() {
                            document.getElementById('btnSave').disabled = false;
                            document.getElementById('btnCancel').disabled = false;
                            document.getElementById('btnSave').innerHTML = 'Save';
                            TimeKeep.remoteLock = 0;
                        }, 500);
                    };

                    if (TimeKeep.editMode === 'A') {

                        TimeKeep.sendRequest('/timekeepentries', 'POST', data,
                            function success(result) {
                                if (!result || !result.data) {
                                    return; // TODO; handle
                                }
                                if (result.data.Modified) {
                                    var modchange = { changeType: 'Modified', data: result.data.Modified };
                                    TimeKeep.broadcastChange(modchange);
                                    TimeKeep.changes.push(modchange);
                                }
                                var newchange = { changeType: 'Added', data: result.data.New };
                                TimeKeep.changes.push(newchange);
                                TimeKeep.broadcastChange(newchange);
                                TimeKeep.editMode = null;
                                TimeKeep.megaUpdate();
                            },
                            function error(result) {
                                // TODO: Handle
                            },
                            done
                        );
                    } else if (TimeKeep.editMode === 'E') {
                        TimeKeep.sendRequest('/timekeepentries', 'PUT', data,
                            function success(result) {
                                if (!result || !result.data) {
                                    return; // TODO; handle
                                }
                                var change = { changeType: 'Modified', data: result.data };
                                TimeKeep.changes.push(change);
                                TimeKeep.broadcastChange(change);
                                TimeKeep.editMode = null;
                                TimeKeep.editID = null;
                                TimeKeep.megaUpdate();
                            },
                            function error(result) {
                                // TODO: Handle
                            },
                            done
                        );

                    } else if (TimeKeep.editMode === 'D') {
                        TimeKeep.sendRequest('/timekeepentries', 'DELETE', data,
                            function success(result) {
                                if (!result || !result.data) {
                                    return; // TODO; handle
                                }
                                var change = { changeType: 'Deleted', data: result.data };
                                TimeKeep.changes.push(change);
                                TimeKeep.broadcastChange(change);
                                TimeKeep.editMode = null;
                                TimeKeep.editID = null;
                                TimeKeep.megaUpdate();
                            },
                            function error(result) {
                                // TODO: Handle
                            },
                            done
                        );
                    }
                }
            },
            btnLogClick: function (sender) {

                // Fix: This operation apparently has "concurrency" issues, so decided to change up the locking mechanism
                if (TimeKeep.remoteLock === 0) {
                    if (++TimeKeep.remoteLock >= 2) {
                        --TimeKeep.remoteLock;
                        return;
                    }
                }
                else
                    return;


                this.btnCloseSummaryClick();
                var id = sender.parentElement.parentElement.id;
                var url = ['/timekeepentries/', id, '/toggle/islogged'].join('');
                TimeKeep.sendRequest(url, 'PATCH', null,
                    function success(result) {
                        if (result && result.data) {
                            var change = { changeType: 'Modified', data: result.data };
                            TimeKeep.changes.push(change);
                            TimeKeep.broadcastChange(change);
                            TimeKeep.processChanges();
                        }
                        else {
                            // TODO: Handle
                        }
                    },
                    function error(result) {
                        // TODO: Handle
                    },
                    function done() {
                        TimeKeep.remoteLock = 0;
                    }
                );
            },
            btnNewClick: function (sender) {
                this.btnCloseSummaryClick();
                TimeKeep.editMode = 'A';
                TimeKeep.firstEdit = true;
                TimeKeep.editID = sender.parentNode.parentNode.id;
                TimeKeep.megaUpdate();
            },
            btnEditClick: function (sender) {
                this.btnCloseSummaryClick();
                TimeKeep.editMode = 'E';
                TimeKeep.editID = sender.parentNode.parentNode.id;
                TimeKeep.firstEdit = true;
                TimeKeep.megaUpdate();
            },
            btnDeleteClick: function (sender) {
                this.btnCloseSummaryClick();
                TimeKeep.editMode = 'D';
                TimeKeep.editID = sender.parentNode.parentNode.id;
                TimeKeep.megaUpdate();
            },
            btnSummaryClick: function (sender) {

                var caseNumber = document.getElementById(sender.parentNode.parentNode.id).data.CaseNumber;

                document.getElementById('tbodySummary').innerHTML = '<tr><td colspan="4">Loading...</td></tr>';
                document.getElementById('divCaseSummary').currentCase = caseNumber;
                var laborText = document.getElementById('spanTotalUnloggedLaborText');
                var laborSpan = document.getElementById('spanTotalUnloggedLabor');
                laborText.style.display = 'none';

                TimeKeep.sendRequest('/timekeepentries/case/' + caseNumber + '/totals', 'POST', TimeKeep.getUTCDateRange(),
                    function success(result) {
                        if (!result || !result.data) {
                            return; // TODO; handle
                        }

                        document.getElementById('spanTotalLabor').innerHTML = result.data.TotalLabor;
                        if (result.data.TotalUnloggedLabor !== '00:00:00') {
                            laborText.style.display = '';
                            laborSpan.innerHTML = result.data.TotalUnloggedLabor;
                        }
                        document.getElementById('divCaseSummary').style.display = '';

                        TimeKeep.sendRequest('/timekeepentries/case/' + caseNumber, 'POST', TimeKeep.getUTCDateRange(),
                            function success(result) {
                                if (!result || !result.data) {
                                    return; // TODO; handle
                                }

                                var sb = [];
                                sb.push('<tr>');
                                for (var i = 0; i < result.data.length; i++) {
                                    var entry = result.data[i];
                                    sb.push('<td>');
                                    if (entry.StartTime)
                                        sb.push(new Date(entry.StartTime).toTime());
                                    sb.push('</td>');
                                    sb.push('<td>');
                                    if (entry.EndTime)
                                        sb.push(new Date(entry.EndTime).toTime());
                                    sb.push('</td>');
                                    sb.push('<td>');
                                    if (entry.Category) {
                                        sb.push(entry.Category.Description);
                                    }
                                    sb.push('</td>');
                                    sb.push('<td>');
                                    sb.push(entry.Labor);
                                    sb.push('</td>');
                                    sb.push('</tr>');
                                }

                                document.getElementById('tbodySummary').innerHTML = sb.join('');
                            },
                            function error(result) {
                                // TODO: Handle
                            }
                        );
                    },
                    function error(result) {
                        // TODO: Handle
                    }
                );
            },
            btnLogAndDetailAllClick: function (sender) {
                var caseNum = document.getElementById('divCaseSummary').currentCase;
                document.getElementById('btnLogAndDetailAll').disabled = true;
                document.getElementById('btnLogAndDetailAll').innerHTML = 'Saving...';
                var done = function () {
                    setTimeout(function () {
                        document.getElementById('btnLogAndDetailAll').disabled = false;
                        document.getElementById('btnLogAndDetailAll').innerHTML = 'Log all entries';
                    }, 599);
                };
                if (caseNum && caseNum.length) {
                    TimeKeep.sendRequest('/timekeepentries/case/' + caseNum + '/LogAndDetailAll', 'PUT', TimeKeep.getUTCDateRange(),
                        function success(result) {
                            document.getElementById('divCaseSummary').style.display = 'none';
                            if (!result || !result.data) {
                                return; // TODO; handle
                            }

                            for (var i = 0; i < result.data.length; i++) {
                                var modchange = { changeType: 'Modified', data: result.data[i] };
                                TimeKeep.broadcastChange(modchange);
                                TimeKeep.changes.push(modchange);
                            }

                            TimeKeep.processChanges();
                        },
                        function error(result) {
                            // TODO: Handle
                        },
                        done
                    );
                }
            },
            btnCloseSummaryClick: function () {
                document.getElementById('divCaseSummary').style.display = 'none';
            },
            createRow: function (entry) {
                var row = document.createElement('tr');
                row.id = entry.ID;
                if (entry.Category && entry.Category.IsScorecard) {
                    if (!entry.IsLogged)
                        row.className = 'entry_red';
                    else
                        row.className = 'entry_green';
                }
                row.innerHTML = TimeKeep.createRowCells(entry);
                row.data = entry;
                return row;
            },
            createRowCells: function (entry) {
                var sb = [];
                sb.push('<td>');
                if (entry.StartTime)
                    sb.push(new Date(entry.StartTime).toTime());
                sb.push('</td>');
                sb.push('<td>');
                if (entry.EndTime)
                    sb.push(new Date(entry.EndTime).toTime());
                sb.push('</td>');
                if (entry.Category) {
                    sb.push('<td id="');
                    sb.push(entry.Category.ID);
                    sb.push('">');
                    sb.push(entry.Category.Description);
                }
                else {
                    sb.push('<td>');
                }
                sb.push('</td>');
                isScorecard = entry.CaseNumber && entry.CaseNumber.length;
                if (isScorecard) {
                    sb.push('<td>');
                    sb.push(entry.CaseNumber);
                    sb.push('</td>');
                }
                else {
                    sb.push('<td></td>')
                }
                sb.push('<td>');
                sb.push(entry.Labor);
                sb.push('</td>');
                sb.push('<td>');
                sb.push(entry.IsLogged ? 'Yes' : 'No');
                sb.push('</td>');
                sb.push('<td>');
                if (!entry.EndTime) {
                    sb.push(' <button onclick="TimeKeep.btnNewClick(this);" class="stopnlog">Stop and Log</button>');
                }
                else {
                    sb.push('<button onclick="TimeKeep.btnLogClick(this);">');
                    sb.push(entry.IsLogged ? 'Unlog' : 'Log');
                    sb.push('</button>');
                    sb.push(' <button onclick="TimeKeep.btnEditClick(this);">Edit</button>');
                    sb.push(' <button onclick="TimeKeep.btnDeleteClick(this);">Delete</button>');
                    if(isScorecard)
                        sb.push(' <button onclick="TimeKeep.btnSummaryClick(this);">Summary</button>'); 
                }
                sb.push('</td>');
                return sb.join('');
            },
            megaUpdate: function () {

                if (TimeKeep.editMode) {
                    if (TimeKeep.editMode === 'A') {
                        document.getElementById('StartTimeHH').parentNode.style.display = 'none';
                        document.getElementById('EndTimeHH').parentNode.style.display = 'none';
                        document.getElementById('Category').parentNode.style.display = '';
                        document.getElementById('CaseNumber').parentNode.style.display = '';
                        document.getElementById('popupheader').innerHTML = 'Stop and Log';
                        document.getElementById('caption').innerHTML = 'Before starting a new entry, please enter the information for your previous entry.';
                        document.getElementById('btnSave').innerHTML = 'Save';
                        if (TimeKeep.firstEdit) {
                            document.getElementById('StartTimeHH').selectedIndex = 0;
                            document.getElementById('StartTimeMM').selectedIndex = 0;
                            document.getElementById('StartTimeSS').selectedIndex = 0;
                            document.getElementById('StartTimeAM').selectedIndex = 0;
                            document.getElementById('EndTimeHH').selectedIndex = 0;
                            document.getElementById('EndTimeMM').selectedIndex = 0;
                            document.getElementById('EndTimeSS').selectedIndex = 0;
                            document.getElementById('EndTimeAM').selectedIndex = 0;

                            document.getElementById('Category').options[TimeKeep.generalAdminIdx].selected = true;
                            document.getElementById('CaseNumber').value = '';
                            TimeKeep.firstEdit = false;
                        }
                    }
                    else if (TimeKeep.editMode === 'E') {
                        document.getElementById('StartTimeHH').parentNode.style.display = '';
                        document.getElementById('EndTimeHH').parentNode.style.display = '';
                        document.getElementById('Category').parentNode.style.display = '';
                        document.getElementById('CaseNumber').parentNode.style.display = '';
                        document.getElementById('popupheader').innerHTML = 'Edit Entry';
                        document.getElementById('caption').innerHTML = 'You are modifying an existing entry.';
                        document.getElementById('btnSave').innerHTML = 'Save';

                        if (TimeKeep.firstEdit) {
                            var entry = document.getElementById(TimeKeep.editID).data;
                            var startTime = new Date(Date.parse(entry.StartTime));
                            var endTime = new Date(Date.parse(entry.EndTime));

                            var hour = startTime.getHours();
                            document.getElementById('StartTimeAM').value = hour >= 0 && hour < 12 ? 'AM' : 'PM';
                            document.getElementById('StartTimeMM').value = startTime.getMinutes();
                            document.getElementById('StartTimeSS').value = startTime.getSeconds();
                            if (hour === 0) hour = 12;
                            else if (hour > 12) hour -= 12;
                            document.getElementById('StartTimeHH').value = hour;

                            hour = endTime.getHours();
                            document.getElementById('EndTimeAM').value = hour >= 0 && hour < 12 ? 'AM' : 'PM';
                            document.getElementById('EndTimeMM').value = endTime.getMinutes();
                            document.getElementById('EndTimeSS').value = endTime.getSeconds();
                            if (hour === 0) hour = 12;
                            else if (hour > 12) hour -= 12;
                            document.getElementById('EndTimeHH').value = hour;

                            document.getElementById('Category').value = entry.Category.ID;
                            document.getElementById('CaseNumber').value = entry.CaseNumber;

                            TimeKeep.firstEdit = false;
                        }
                    }
                    else if (TimeKeep.editMode === 'D') {
                        document.getElementById('StartTimeHH').parentNode.style.display = 'none';
                        document.getElementById('EndTimeHH').parentNode.style.display = 'none';
                        document.getElementById('Category').parentNode.style.display = 'none';
                        document.getElementById('CaseNumber').parentNode.style.display = 'none';
                        document.getElementById('popupheader').innerHTML = 'Delete Entry';
                        document.getElementById('caption').innerHTML = 'Are you sure you want to delete this entry?';
                        document.getElementById('btnSave').innerHTML = 'Delete';
                    }
                    document.getElementById('popup').style.display = '';
                    document.getElementById('btnSYD').disabled = true;

                    if (!TimeKeep.errors || !TimeKeep.errors.length) {
                        document.getElementById('errors').style.display = 'none';
                    } else {

                        var sb = [];
                        for (var i = 0; i < TimeKeep.errors.length; i++) {
                            sb.push('<li>');
                            sb.push(TimeKeep.errors[i]);
                            sb.push('</li>');
                        }

                        document.getElementById('errorList').innerHTML = sb.join('');
                        sb = null;

                        document.getElementById('errors').style.display = '';
                    }
                }
                else {
                    document.getElementById('errors').style.display = 'none';
                    document.getElementById('popup').style.display = 'none';
                    document.getElementById('btnSYD').disabled = false;
                }

                document.getElementById('popup').style.display = TimeKeep.editMode ? '' : 'none';

                var cat = document.getElementById('Category').options[document.getElementById('Category').selectedIndex].parentNode.id;
                if (cat === 'CategoryScorecard') {
                    document.getElementById('CaseNumber').disabled = false;
                }
                else {
                    document.getElementById('CaseNumber').disabled = true;
                    document.getElementById('CaseNumber').value = '';
                }

                if (TimeKeep.categoriesLoaded) {
                    document.getElementById('btnSave').disabled = false;
                }

                TimeKeep.processChanges();
            },
            processChanges: function () {
                if (TimeKeep.changes && TimeKeep.changes.length) {
                    while (TimeKeep.changes.length) {
                        var change = TimeKeep.changes.shift();
                        var row = null;
                        if (change.changeType === 'Modified') {
                            if (document.getElementById('noentries').style.display !== 'none') {
                                document.getElementById('noentries').style.display = 'none';
                                document.getElementById('entries').style.display = '';
                            }
                            row = document.getElementById(change.data.ID);
                            newRow = TimeKeep.createRow(change.data);
                            row.parentNode.insertBefore(newRow, row);
                            row.parentNode.removeChild(row);
                        } else if (change.changeType === 'Deleted') {
                            row = document.getElementById(change.data.ID);
                            row.parentNode.removeChild(row);
                        } else if (change.changeType === 'Added') {
                            if (!document.getElementById('tbEntries').length) {
                                document.getElementById('noentries').style.display = 'none';
                                document.getElementById('entries').style.display = '';
                            }
                            document.getElementById('tbEntries').appendChild(TimeKeep.createRow(change.data));
                        } else if (change.changeType === 'EndOfDay') {
                            document.getElementById('tbEntries').innerHTML = '';
                            document.getElementById('entries').style.display = 'none';
                            document.getElementById('noentries').style.display = '';
                        }
                    }
                    TimeKeep.updateTotals();
                }
            },
            updateTotals: function () {
                TimeKeep.sendRequest('/timekeepentries/user/' + TimeKeep.user + '/totals', 'POST', TimeKeep.getUTCDateRange(),
                    function (result) {
                        if (result && result.data && result.data.length) {
                            var sb = [];
                            for (var i = 0; i < result.data.length; i++) {
                                var item = result.data[i];
                                if (i > 0)
                                    sb.push('&nbsp;&nbsp;&nbsp;');
                                sb.push('<span>');
                                sb.push(item.Category.Description);
                                sb.push(': ');
                                sb.push(item.Labor);
                                sb.push('</span>');
                            }
                            document.getElementById('tdtotals').innerHTML = sb.join('');
                        }
                        else {
                            // TODO: Handle
                        }
                    },
                    function (result) {
                        // TODO: Handle
                    });
            },
            endOfDay: function () {
                TimeKeep.changes = [];
                var change = { data: null, changeType: "EndOfDay" };
                TimeKeep.broadcastChange(change);
                TimeKeep.changes.push(change);
                processChanges();
            }
        };

        initPara = null;

        window.TimeKeep = new __global();
    }
)();