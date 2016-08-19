var diffViewScript = function (currentFilePath, headSha, pathPrefix, sourceFileTree, repoOwner, repoName) {

   


    var cachedFileTree = sourceFileTree;
    //will be swapped out
    var reloadEditors = null;



    var tree;
    var treeHome;
    var targetEditor;
    var sourceEditor;
    var comments = null;
    var options = lscache.get("options") || { showInlineComments: true, splitterWidth: ($(window).width() * 0.2) };

    $('#show-inline-comments').attr('checked', options.showInlineComments);

    $('#show-inline-comments').change(function () {
        var checked = $(this).is(':checked');
        options.showInlineComments = checked;
        lscache.set("options", options);
        applyCommentsToEditors();
    });
    $('.apply-md').each(function () {
        var $this = $(this);
        var innerHtml = $this.html();
        
        $this.parent().html(marked(innerHtml));
    })

    function reload() {
        loadFileList();
        loadPath(currentFilePath + '#' + window.location.hash.replace('#', ''), false, true);

        loadComments();
    }
    function checkVersions(obj) {
        //if (obj.headSha == headSha) {
        //    return true;
        //} else {
        //    headSha = obj.headSha;
        //    setTimeout(function () {

        //        reload()
        //    }, 1);
        //    return true;
        //}
        return true;
    }

    function convertHunkToHtml(hunk) {
        //first line is @@???@@
        var lines = hunk.split('\n');
        html = "<table>";
        var sLine = 0;
        var tLine = 0;
        var rgex = new RegExp("^\\@\\@\\s?(?:-(\\d*),(\\d*))?\\s?(?:\\+(\\d*),(\\d*))?\\s?\\@\\@");
        for (var i in lines) {
            var line = lines[i];
            if (line[0] == '@') {

                var result = rgex.exec(line);
                sLine = result[1] - 1;
                tLine = result[3] - 1;
            } else {

                var cls = '';
                if (line[0] == '-') {
                    cls = 'removed';
                } else if (line[0] == '+') {
                    cls = 'added';
                }

                html += "<tr class='" + cls + "'>";
                html += "<td class='src'>";
                if (line[0] == ' ' || line[0] == '-') {
                    sLine++;
                    html += sLine;
                }
                html += "</td>";

                html += "<td class='target'>";
                if (line[0] == ' ' || line[0] == '+') {
                    tLine++;
                    html += tLine;
                }
                html += "</td>";

                html += "<td class='txt'>";
                html += line.substring(1);
                html += "</td>";



                html += "</tr>";
            }
        }
        html += "</table>";
        return html;
    }

    var filesNodes = {};

    function loadFileList() {
        filesNodes = {};
        function fixNode(node) {
            node.state = {};
            if (node.path === currentFilePath) {
                blankState = node;
                lscache.set(node.sha, true, 60 * 24 * 7 * 52);
                node.state.selected = true;
            }

            //due to the recursive nature this will always be the lowest item
            if (node.path === "") {

                node.href = pathPrefix;
                node.icon = 'icon icon-git-pull-request';
                node.class = "pullrequest-home";
                node.nodes = node.children;
            } else if (node.hasOwnProperty('path')) { //has a path then its a file
                filesNodes[node.path] = node;
                node.href = pathPrefix + '/files/' + node.path;
                node.icon = 'icon icon-file-text';
                if (lscache.get(node.sha)) {
                    node.class = "visisted";
                } else {
                    node.class = "not-visisted";
                }
                node.class += " status-" + node.change;

                node.id = node.path;

            } else {
                node.nodes = node.children;
                node.icon = 'icon icon-file-directory';
                //  node.expandedIcon = 'glyphicon glyphicon-folder-open';
                node.selectable = false;
                node.state.expanded = true;
            }

            if (node.nodes && node.nodes.length > 0) {

                var commonChange = node.nodes[0].change;

                for (var i = 0; i < node.nodes.length; i++) {
                    var nChange = node.nodes[i].change;
                    if (nChange != commonChange) {
                        commonChange = 'modified';
                    }

                    fixNode(node.nodes[i]);
                }
                node.class = "status-" + commonChange;
            }
        }

        var files = cachedFileTree;
        if (!checkVersions(files)) {
            return;
        }

        var rootNode = {
            path: "",
            text: "Pull Request",
            children: files.data,
        };

        fixNode(rootNode);
        var nodes = rootNode.nodes;
        //  nodes.unshift(rootNode)
        //rootNode.nodes = null;
        //rootNode.children = null;


        $('#tree').treeview({
            data: nodes,
            showTags: true,
            expandIcon: 'icon icon-chevron-down',
            collapseIcon: 'icon icon-chevron-right',
            emptyIcon: 'icon',
            expandOptions: {
                ignoreChildren: true
            }
            //collapseIcon :'glyphicon glyphicon-folder-open',
            //expandIcon :'glyphicon glyphicon-folder-close',
        }).on('nodeSelected', function (e, node) {
            if (node.path) {
                var node = tree.getNode(node.nodeId)
                node.class = node.class.replace("not-visisted", "visisted");
                lscache.set(node.sha, true, 60 * 24 * 7 * 52);
            }
            setTimeout(function () {
                loadPath(node.path);
            }, 1);
        })
        .on('nodeUnselected', function (e, node) {

            setTimeout(function () {
                //wait for followup selectinoto have a change to propergate
                if (currentFilePath != '') {
                    var sel = tree.getSelected();
                    if (!sel || !sel.length) {
                        tree.selectNode(node.nodeId);//reselect the last selected item
                    }
                }
            }, 1);
        });
        treeHome = $('.pullrequest-home');
        tree = $('#tree').treeview(true);
        //ajax  call to load the treeView
        applyCommentsToTree();


    }

    function loadComments(cb) {
        $.get(pathPrefix + '/comments?expectedSha=' + headSha, function (result) {

            if (!checkVersions(result)) {
                return;
            }

            var data = result.comments;
            var templateMain = $('#home-comment-template').html();
            var templateFileBlock = $('#home-file-comment-template').html();
            var templateFile = $('#inline-file-comment-template').html();
            $('#home .event').remove();
            var home = $('#home');
            comments = {};
            var fileDiffSlots = {};
            for (var i in data) {
                var comment = data[i];
                var path = comment.path || '';
                if (!comments[path]) {
                    comments[path] = [];
                }
                comments[path].push(comment);
                var template = templateMain;
                if (comment.path) {
                    var lineLink = '';
                    if (comment.sourceLine) {
                        lineLink = 's-' + (comment.sourceLine + 1);
                    } else if (comment.targetLine) {
                        lineLink = 't-' + (comment.targetLine + 1);
                    }

                    var lineComments = fileDiffSlots[comment.path]
                    if (!lineComments) {
                        fileDiffSlots[comment.path] = lineComments = {};
                    }
                    var lineCommnet = lineComments[lineLink];
                    if (!lineCommnet) {
                        var html = templateFileBlock
                        .replaceAll("{path}", comment.path)
                        .replaceAll("{change}", comment.change)
                        .replaceAll("{fullPath}", pathPrefix + '/files/' + comment.path)
                        .replaceAll("{lineLink}", lineLink)
                        .replaceAll("{avatarUrl}", comment.user.avatarUrl)
                        .replaceAll("{username}", comment.user.login)
                        .replaceAll("{created}", comment.createdAt)
                        .replaceAll("{patch}", convertHunkToHtml(comment.patch))
                        .replaceAll("{body}", marked(comment.body));
                        fileComments = $(html);
                        home.append(fileComments);
                        lineComments[lineLink]  = fileComments;
                    }

                    var html = templateFile
                    .replaceAll("{path}", comment.path)
                        .replaceAll("{change}", comment.change)
                    .replaceAll("{fullPath}", pathPrefix + '/files/' + comment.path)
                    .replaceAll("{lineLink}", lineLink)
                    .replaceAll("{avatarUrl}", comment.user.avatarUrl)
                    .replaceAll("{username}", comment.user.login)
                    .replaceAll("{created}", comment.createdAt)
                    .replaceAll("{body}", marked(comment.body));
                    fileComments.append(html)
                } else {
                    var html = template
                        .replaceAll("{avatarUrl}", comment.user.avatarUrl)
                        .replaceAll("{change}", comment.change)
                        .replaceAll("{username}", comment.user.login)
                        .replaceAll("{created}", comment.createdAt)
                        .replaceAll("{body}", marked(comment.body))

                    home.append(html)
                }
                
            }

            $('.timeago').timeago();
            fixHeights();
            applyCommentsToTree();
            applyCommentsToEditors();

            if (cb) {
                cb();
            }
            //load comments into main screen


        });
    }

    function applyCommentsToTree() {
        if (!comments) {
            return;
        }

        if (!tree) {
            return
        }

        var nodes = tree.getEnabled();

        var rootNode = null;
        var total = 0;
        for (var n in nodes) {
            var node = nodes[n];
            if (node.path == "") {
                rootNode = node;
            }

            if (comments[node.path] && comments[node.path].length > 0) {
                node.tags = [comments[node.path].length];

                total += comments[node.path].length
            } else { node.tags = []; }

        }
        if (total > 0) {
            $('#file-list .header .badge').show();
            $('#file-list .header .badge').text(total);
        }

        tree.redraw();

    }

    function applyCommentsToEditors() {

        if (!comments) {
            return;
        }
        //clear currrently appled comments from system

        function removeCommentBlocks(editor) {
            if (!editor) {
                return;
            }
            var doc = editor.getDoc();
            for (var i in doc._comments) {
                doc._comments[i].widget.clear();
            }
            doc._comments = null;
        }
        removeCommentBlocks(targetEditor);
        removeCommentBlocks(sourceEditor);

        if (options.showInlineComments) {
            var templateFile = $('#inline-file-comment-template').html();

            function addComment(doc, line, comment) {
                doc._comments = doc._comments || {};

                var block = doc._comments[line];

                if (!block) {
                    var elm = $('<div class="commentList" />');
                    var widget = doc.addLineWidget(line, elm[0], { coverGutter: true, noHScroll: true, });
                    doc._comments[line] = block = { elm: elm, widget: widget };
                }

                var html = templateFile
                    .replaceAll("{path}", comment.path)
                    .replaceAll("{fullPath}", pathPrefix + '/files/' + comment.path)
                    .replaceAll("{avatarUrl}", comment.user.avatarUrl)
                    .replaceAll("{username}", comment.user.login)
                        .replaceAll("{change}", comment.change)
                    .replaceAll("{created}", comment.createdAt)
                    .replaceAll("{body}", marked(comment.body))

                block.elm.append(html);
                block.widget.changed();
                //dot adda single widget per comment append to the old widget if exists

            }

            var targetDoc = null;
            if (targetEditor) {
                targetDoc = targetEditor.getDoc();
            }
            var sourceDoc = null;
            if (sourceEditor) {
                var sourceDoc = sourceEditor.getDoc();
            }
            var fileComments = comments[currentFilePath];
            if (fileComments) {
                for (var i in fileComments) {

                    var comment = fileComments[i];
                    if (sourceDoc) {
                        if (comment.sourceLine) {
                            addComment(sourceDoc, comment.sourceLine, comment);
                        }
                    }
                    if (targetDoc) {
                        if (comment.targetLine) {

                            addComment(targetDoc, comment.targetLine, comment);
                        }
                    }
                }
            }
        }

        $('#file_diff .timeago').timeago();

        if (reloadEditors) {
            reloadEditors();

        }
        setTimeout(function () {
            scrollToLine(currentLine);
        }, 1);
    }
    //preiodically reload comments and reapply??? on loadPageMaybe???
   

    function inversePatch(patch) {
        for (var i = 0; i < patch.length; i++) {
            var p = patch[i];
            for (var j = 0; j < p.hunks.length; j++) {
                var h = p.hunks[j];
                for (var k = 0; k < h.lines.length; k++) {
                    var line = h.lines[k];

                    if (line[0] == '-') {
                        line= '+' + line.substring(1);
                    }else if (line[0] == '+') {
                        line = '-' + line.substring(1);
                    }

                    h.lines[k] = line;
                }

                var t = h.newLines;
                h.newLines = h.oldLines;
                h.oldLines = t;

                var t = h.newStart;
                h.newStart = h.oldStart;
                h.oldStart = t;
            }
        }
        return patch;
    }

    var currentLine = '';
    //called directly
    function loadPath(path, skipNavigation, refresh) {

        var parts = path.split('#')
        path = parts[0];
        var lineScrollerTarget = parts[1];



        if (!skipNavigation) {
            var navPath = path;
            var statePath = path;
            if (navPath) {
                navPath = '/files/' + navPath;
            }

            var urlLine = lineScrollerTarget;
            if (parts.length == 1 && currentFilePath == path)
            {
                //we stayed on the same page but we didn't select a new line then pick old line to render on the url
                urlLine = currentLine;
            }

            if (urlLine) {
                navPath = navPath + '#' + urlLine;
                statePath = statePath + '#' + urlLine;
            }

            currentLine = urlLine;



            if (currentFilePath != path) {
                history.pushState({ path: statePath }, null, pathPrefix + navPath);
            } else {
                history.replaceState({ path: statePath }, null, pathPrefix + navPath);
            }
        }

        if (currentFilePath == path && !refresh) {
            scrollToLine(lineScrollerTarget);

            return; //we are allready showing the page remain
        }
        currentFilePath = path;
        targetEditor = null;
        sourceEditor = null;
        commentBlocks = {};//reset the applied comments

        var targetElm = $('#file_diff').hide();
        var editorElm = $('#file_diff .editor');
        var loaderElm = $('#loader').show();
        var homeElm = $('#home').hide();

        //we need to reload the comments whiel we are loading the doc

        setTimeout(function () { 

        if (tree) {
            var nodeFound = false;
            var nodes = tree.getEnabled();

            for (var n in nodes) {
                var node = nodes[n];
                if (node.path == path) {
                    tree.selectNode(node.nodeId);
                    nodeFound = true;
                    break;
                } else {
                    tree.unselectNode(node.nodeId);
                }
            }
            tree.redraw();
        }



        if (!path) {
            homeElm.show();
            loaderElm.hide();
            fixHeights();
            return;
        }
        function serverResult(data) {

            if (!checkVersions(data)) {
                return;
            }

            $('#fileName').text(path);
            $('#fileName').removeClass("status-modified");
            $('#fileName').removeClass("status-added");
            $('#fileName').removeClass("status-removed");
            $('#fileName').addClass("status-" + data.change);
            if (data.notfound) {
                editorElm.html("<div>Unable to find '" + path + "' in pull request.</div>");
                return;
            }
            if (data.isBinary) {
                editorElm.html("<div>Binary file " + data.change + ", preview unavailible.</div>");
                return;
            }

            editorElm.html("");
            targetElm.show();
            loaderElm.hide();



            var fileMode = CodeMirror.findModeByFileName(path);

            
            var targetText = data.target || "";
            var sourceText = "";
            if (filesNodes[path].change != 'added') {
                var patch = JsDiff.parsePatch(filesNodes[path].patch);
                sourceText = JsDiff.applyPatch(targetText, inversePatch(patch));
            }

           

            var mime = "";
            if (fileMode) {
                mime = fileMode.mode;
            }
            reloadEditors = null;
            if (!sourceText) {

                targetEditor = CodeMirror(editorElm[0], {
                    value: targetText,
                    lineNumbers: true,
                    mode: mime,
                    connect: null,
                    readOnly: 'nocursor',
                    allowEditingOriginals: false,
                    collapseIdentical: false,
                    revertButtons: false,
                    lineWrapping: true,
                    gutters: ["CodeMirror-linenumbers", "github-comments"]
                });
                reloadEditors = function () {
                    targetEditor.refresh();
                }
                var doc = targetEditor.getDoc();

                var lineCount = doc.lineCount();
                var lastListChar = doc.getLine(lineCount - 1).length;
                doc.markText({ line: 0, ch: 0 }, { line: lineCount, ch: lastListChar }, { className: 'CodeMirror-merge-l-inserted', inclusiveLeft: true, inclusiveRight: true });
                //single page code mirror with all green background
            } else if (!targetText) {

                //single page code mirror with all red background
                sourceEditor = CodeMirror(editorElm[0], {
                    value: sourceText,
                    lineNumbers: true,
                    mode: mime,
                    connect: null,
                    readOnly: 'nocursor',
                    allowEditingOriginals: false,
                    collapseIdentical: false,
                    revertButtons: false,
                    lineWrapping: true,
                    gutters: ["CodeMirror-linenumbers", "github-comments"]
                });
                reloadEditors = function () {
                    sourceEditor.refresh();
                }
                var doc = sourceEditor.getDoc();
                var lineCount = doc.lineCount();
                var lastListChar = doc.getLine(lineCount - 1).length;
                doc.markText({ line: 0, ch: 0 }, { line: lineCount, ch: lastListChar }, { className: 'CodeMirror-merge-l-deleted', inclusiveLeft: true, inclusiveRight: true });
            } else {
                var mergView = CodeMirror.MergeView(editorElm[0], {
                    origLeft: sourceText,
                    value: targetText,
                    lineNumbers: true,
                    mode: mime,
                    connect: null,
                    readOnly: 'nocursor',
                    allowEditingOriginals: false,
                    collapseIdentical: false,
                    revertButtons: false,
                    lineWrapping: true,
                    gutters: ["CodeMirror-linenumbers", "github-comments"]
                });

                sourceEditor = mergView.left.orig;
                targetEditor = mergView.edit;
                reloadEditors = function () {
                    sourceEditor.refresh();
                    targetEditor.refresh();
                    mergView.resize();
                }

            }
            if (fileMode) {
                if (sourceEditor) {
                    CodeMirror.autoLoadMode(sourceEditor, fileMode.mode);//call this once loaded
                }
                if (targetEditor) {
                    CodeMirror.autoLoadMode(targetEditor, fileMode.mode);//call this once loaded
                }
            }
            function markPatch(editor, map) {
                if (editor) {
                    var doc = editor.getDoc();
                    var lineCount = doc.lineCount();

                    var annotation = [];
                    var currentAnn = null;
                    for (var i = 0; i < lineCount; i++) {
                        var mappedPage = map[i + 1]
                        if (mappedPage > -1) {
                            if (currentAnn == null) {
                                currentAnn = { from: { line: i } };
                            }
                            currentAnn.to = { line: i };
                            //var btn = $('<span class="addcomment"></span>');
                            //btn.click(function () {
                            //    addComments(this);
                            //});

                            //editor.setGutterMarker(i, "github-comments", btn[0]);
                            //btn.data("patchLine", mappedPage);

                        } else {
                            if (currentAnn) {
                                annotation.push(currentAnn);
                            }
                            currentAnn = null;
                            doc.addLineClass(i, "wrap", "not-in-patch");
                        }
                    }
                    if (currentAnn !== null) {
                        annotation.push(currentAnn);
                    }
                    var bar = editor.annotateScrollbar('in-patch');

                    bar.update(annotation);
                }
            }

            //markPatch(targetEditor, data.pageMap.TargetFile);
            //markPatch(sourceEditor, data.pageMap.SourceFile);

            var lineClicked = function (cm, line, gutter) {
                var type = 's';
                if (cm == targetEditor) {
                    type = 't';
                }
                var target = type + '-' + (line + 1);
                if (currentLine == target){
                    loadPath(path + '#');//removes the selected line !!!
                }else{
                    loadPath(path + '#' + target);
                }
            }

            if (targetEditor) { targetEditor.on("gutterClick", lineClicked); }
            if (sourceEditor) { sourceEditor.on("gutterClick", lineClicked); }

            var targetPos = $('#splitter').position().top;
            var mainSCroller = $("html, body");

            var currentTarget = 0;
            var scrollerTimeout;
            var scroll = function (cm) {
                var info = cm.getScrollInfo();
                var curentPos = info.top;
                if (window.scrollY < targetPos) {
                    mainSCroller.stop(true);
                    if (info.top > 5) {
                        mainSCroller.animate({ 'scrollTop': targetPos + 'px' });
                    }
                }
            }

            //editorElm.find('.CodeMirror-scroll').on('scroll', function (e) {
            //    e.preventDefault();

            //});

            if (targetEditor) { targetEditor.on("scroll", scroll); }
            if (sourceEditor) { sourceEditor.on("scroll", scroll); }


            applyCommentsToEditors();
            fixHeights();
            scrollToLine(lineScrollerTarget);

        }

        if (filesNodes[path] && filesNodes[path].change == 'removed') {
            serverResult({ target: '' });
        } else {

            $.get(pathPrefix + '/contents/' + path + "?expectedSha=" + headSha, serverResult);
        }
        loadComments(function () {
            scrollToLine(lineScrollerTarget);
        });
        }, 10);
    }

    reload();

    window.onpopstate = function (s) {
        var path = s.state.path;
        loadPath(path, true);
    }

    $("#splitter").splitter({
        sizeLeft: options.splitterWidth
    });
    $('#splitter').on("resize", function () {
        options.splitterWidth = $('.left-splitter').width() - 5;
        lscache.set('options', options);
    });

    var topStyle = $("<style />");
    $('head').append(topStyle)

    //var targetPos = $('#splitter').position().top;
    //var mainSCroller = $('html, body');
    //$('#tree').on('scroll', function (e) {


    //    var pos = $(this).scrollTop();

    //    if (window.scrollY < targetPos) {
    //        mainSCroller.stop(true);
    //        if (pos > 2) {
    //            mainSCroller.animate({ 'scrollTop': targetPos + 'px' });
    //        }
    //        e.preventDefault();
    //    }

    //    topStyle.html("#tree .pullrequest-home { top : " + pos + "px} ");
        
    //});

    var fixHeightshomeElm = $('#home');
    var fixHeightsfilediffElm = $('#file_diff');
    var fixHeightssplitterElm = $('#splitter');
    var fixHeightswindowElm = $(window);

    var heightFixerTo;
        function fixHeights() {
            clearTimeout(heightFixerTo);
            heightFixerTo = setTimeout(function () {
                //var offset = splitter.position().top;
                var winh = fixHeightswindowElm.height();

                if (fixHeightshomeElm.is(':visible')) {
                    var viewHeight = winh - fixHeightshomeElm.position().top;
                    var homeHeight = fixHeightshomeElm.outerHeight();


                    fixHeightssplitterElm.height(Math.max(homeHeight, viewHeight)).trigger("resize");
                } else if (fixHeightsfilediffElm.is(':visible')) {
                    console.log('is visisble editor')
                    var editorHEader = fixHeightsfilediffElm.find('.header').outerHeight();
                    $('#mergeHeight').html(".CodeMirror-merge, .CodeMirror-merge .CodeMirror, .CodeMirror { height: " + (winh - editorHEader - 15) + "px;}")

                    fixHeightssplitterElm.height(fixHeightsfilediffElm.outerHeight() + 10).trigger("resize");
                } else {

                }
                //home.height(winh);
            }, 50);
        }
    var to1;
    $(window).resize(function (e) {
        //diffElm .mergely('resize');
        if (e.target === window) {
            clearTimeout(to1);
            to1 = setTimeout(function () {
                fixHeights();
            });
        }
    }).resize();

    $(document).on('click', '[data-naviagetetree]', function (e) {
        e.preventDefault();
        loadPath($(this).attr('data-naviagetetree'));
    })
};