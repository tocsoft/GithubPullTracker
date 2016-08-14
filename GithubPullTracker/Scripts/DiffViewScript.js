var diffViewScript = function (currentFilePath, headSha, pathPrefix, sourceFileTree) {

    var cachedFileTree = sourceFileTree;
    //will be swapped out
    var reloadEditors = null;



    var tree;
    var targetEditor;
    var sourceEditor;
    var comments = null;
    var options = lscache.get("options") || { showInlineComments : true};

    $('#show-inline-comments').attr('checked', options.showInlineComments);

    $('#show-inline-comments').change(function () {
        var checked = $(this).is(':checked');
        options.showInlineComments = checked;
        lscache.set("options", options);
        applyCommentsToEditors();
    });
    

    function reload() {
        loadPath(currentFilePath + '#' + window.location.hash.replace('#', ''), false, true);

        loadFileList();
        loadComments();
    }
    function checkVersions(obj) {
        if (obj.headSha == headSha) {
            return true;
        } else {
            headSha = obj.headSha;
            setTimeout(function () {

                reload()
            }, 1);
            return true;
        }
    }

    function loadFileList() {
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
                node.icon = 'octicon octicon-git-pull-request';

                node.nodes = node.children;
            }else if (node.hasOwnProperty('path')) { //has a path then its a file

                node.href = pathPrefix + '/files/' + node.path;
                node.icon = 'octicon octicon-file';
                if (lscache.get(node.sha)) {
                    node.class = "visisted";
                } else {
                    node.class = "not-visisted";
                }

                node.id = node.path;

            } else {
                node.nodes = node.children;
                node.icon = 'octicon octicon-file-directory';
              //  node.expandedIcon = 'glyphicon glyphicon-folder-open';
                node.selectable = false;
                node.state.expanded = true;
            }

            if (node.nodes && node.nodes.length > 0) {
                for (var i = 0; i < node.nodes.length; i++) {
                    fixNode(node.nodes[i]);
                }
            }
        }
        function loadData(files) {
            if (!checkVersions(files)) {
                return;
            }

            var rootNode = {
                path : "",
                text : "Pull Request",
                children:  files.data,
            };
            
            fixNode(rootNode);
            var nodes = rootNode.nodes;
            nodes.unshift(rootNode)
            rootNode.nodes = null;
            rootNode.children = null;
            

            $('#tree').treeview({
                data: nodes,
                showTags: true,
                expandOptions: {
                    ignoreChildren: true
                }
                //collapseIcon :'glyphicon glyphicon-folder-open',
                //expandIcon :'glyphicon glyphicon-folder-close',
            }).on('nodeSelected', function (e, node) {
                tree.getNode(node.nodeId).class = "visisted";
                lscache.set(node.sha, true, 60 * 24 * 7 * 52);
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

            tree = $('#tree').treeview(true);
            //ajax  call to load the treeView
            applyCommentsToTree();
        }

        if (cachedFileTree) {
            var data = cachedFileTree;
            cachedFileTree = null;
            loadData(data);
        } else {
            $.get(pathPrefix + '/files?expectedSha=' + headSha, loadData);
        }
    }

    String.prototype.replaceAll = function (search, replacement) {
        var target = this;
        return target.replace(new RegExp(search, 'g'), replacement);
    };
    function loadComments(cb) {
        $.get(pathPrefix + '/comments?expectedSha=' + headSha, function (result) {

            if (!checkVersions(result)) {
                return;
            }

            var data = result.comments;
            var templateMain = $('#home-comment-template').html();
            var templateFile = $('#home-file-comment-template').html();
            $('#home .event').remove();
            var home = $('#home');
            comments = {};
            for (var i in data) {
                var comment = data[i];
                var path = comment.path || '';
                if (!comments[path]) {
                    comments[path] = [];
                }
                comments[path].push(comment);
                var template = templateMain;
                if (comment.path) {

                    var  lineLink = '';
                    if (comment.sourceLine) {
                        lineLink = 's-' + (comment.sourceLine+1);
                    } else if (comment.targetLine) {
                        lineLink = 't-' + (comment.targetLine + 1);
                    }


                    template = templateFile
                    .replaceAll("{path}", comment.path )
                    .replaceAll("{fullPath}", pathPrefix + '/files/' + comment.path)
                    .replaceAll("{lineLink}", lineLink);
                }
                var html = template
                    .replaceAll("{avatarUrl}", comment.user.avatarUrl)
                    .replaceAll("{username}", comment.user.login)
                    .replaceAll("{created}", comment.createdAt)
                    .replaceAll("{body}",  marked(comment.body))

               home.append(html)
            }
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

        for (var n in nodes) {
            var node = nodes[n];

            if (comments[node.path] && comments[node.path].length > 0) {
                node.tags = [comments[node.path].length];
            } else { node.tags = []; }

        }


        tree.redraw();

    }

    function applyCommentsToEditors() {

        if (!comments) {
            return;
        }
        //clear currrently appled comments from system
        $('#file_diff .comment').remove();

        if(options.showInlineComments){
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
                        if (comment.sourceLine ) {
                            addComment(sourceDoc, comment.sourceLine, comment);
                        }
                    }
                    if (targetDoc) {
                        if (comment.targetLine ) {

                            addComment(targetDoc, comment.targetLine, comment);
                        }
                    }
                }
            }
        } else {
            function removeCommentBlocks(editor) {
                if (!editor)
                {
                    return;
                }
                var doc = editor.getDoc();
                for (var i in doc._comments) {
                    doc._comments[i].remove();
                }
                doc._comments = null;
            }
            removeCommentBlocks (targetEditor);
            removeCommentBlocks(sourceEditor);
        }
        if (reloadEditors) {
            reloadEditors();
        }
    }
    //preiodically reload comments and reapply??? on loadPageMaybe???
    function scrollToLine(line) {
        if (!line) {
            return;
        }

        var parts = line.split('-');
        var editor = targetEditor;
        if (parts[0] === 's')
        {
            editor = sourceEditor;
        }
        if (editor) {
            var line = parseInt(parts[1]) - 1;
            var t = editor.charCoords({ line: line, ch: 0 }, "local").top;
            var middleHeight = editor.getScrollerElement().offsetHeight / 2;
            editor.scrollTo(null, t - middleHeight - 5);


            if (sourceEditor && sourceEditor._hightlightedLine) {
                sourceEditor.removeLineClass(sourceEditor._hightlightedLine, 'wrap', 'highlighted');
                sourceEditor._hightlightedLine = null;
            }
            if (targetEditor && targetEditor._hightlightedLine) {
                targetEditor.removeLineClass(targetEditor._hightlightedLine, 'wrap', 'highlighted');
                targetEditor._hightlightedLine = null;
            }

            editor.addLineClass(line, 'wrap', 'highlighted');
            editor._hightlightedLine = line;
        }
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
            if (!urlLine && currentFilePath == path) {
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

        var targetElm = $('#file_diff');

        targetElm.html('<div class="loader">Loading ...</div>');

        $('#home').hide();
        targetElm.show();
        //we need to reload the comments whiel we are loading the doc



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
            $('#home').show();
            targetElm.hide();
            return;
        }

        $.get(pathPrefix + '/contents/' + path + "?expectedSha=" + headSha, function (data) {

            if (!checkVersions(data)) {
                return;
            }

            if (data.notfound) {
                targetElm.html("<div>Unable to find '" + path + "' in pull request.</div>");
                return;
            }
            if (data.isBinary) {
                targetElm.html("<div>Binary file " + data.status + ", preview unavailible.</div>");
                return;
            }

            targetElm.html("");//clear the dom

            var fileMode = CodeMirror.findModeByFileName(path);

            var sourceText = data.source;
            var targetText = JsDiff.applyPatch(sourceText, data.patch);

            var mime = "";
            if (fileMode) {
                mime = fileMode.mode;
            }
            reloadEditors = null;
            if (!sourceText) {

                targetEditor = CodeMirror(targetElm[0], {
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
                sourceEditor = CodeMirror(targetElm[0], {
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
                var mergView = CodeMirror.MergeView(targetElm[0], {
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
                            var btn = $('<span class="addcomment"></span>');
                            btn.click(function () {
                                addComments(this);
                            });
                            btn.data("patchLine", mappedPage);
                            editor.setGutterMarker(i, "github-comments", btn[0]);

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

            markPatch(targetEditor, data.pageMap.TargetFile);
            markPatch(sourceEditor, data.pageMap.SourceFile);

            var lineClicked = function (cm, line, gutter) {
                var type = 's';
                if(cm == targetEditor){
                    type = 't';
                }
                loadPath(path + '#' + type + '-' + (line+1));
            }

            if (targetEditor) { targetEditor.on("gutterClick", lineClicked); }
            if (sourceEditor) { sourceEditor.on("gutterClick", lineClicked); }

            applyCommentsToEditors();

        });
        loadComments(function () {
            scrollToLine(lineScrollerTarget);
        });
    }

    reload();

    window.onpopstate = function (s) {
        var path = s.state.path;
        loadPath(path, true);
    }

    var width = $(window).width();
    var multiplier = lscache.get('spliterWidth') || 0.8;
    width = width * multiplier;
    $("#splitter").splitter({
        sizeRight: width
    });
    function fixHeights() {
        var home = $('#home');
        var splitter = $('#splitter');
        var offset = splitter.position().top;
        var winh = $(window).height() - offset;
        splitter.height(winh).trigger("resize");
        home.height(winh);
        $('#mergeHeight').html(".CodeMirror-merge, .CodeMirror-merge .CodeMirror, .CodeMirror { height: " + winh + "px;}")
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