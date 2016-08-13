var diffViewScript = function (currentFilePath, headSha, pathPrefix) {

    //will be swapped out
    var reloadEditors = null;



    var tree;
    var targetEditor;
    var sourceEditor;
    var comments = null;
    $('#details-link').click(function (e) { e.preventDefault(); loadPath(""); });
    function reload() {
        loadPath(currentFilePath, false, true);

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
            //due to the recursive nature this will always be the lowest item


            if (node.path) { //has a path then its a file
                if (node.path === currentFilePath) {
                    blankState = node;
                    lscache.set(node.sha, true, 60 * 24 * 7 * 52);
                    node.state.selected = true;
                }
                node.href = pathPrefix + '/files/' + node.path;
                node.icon = 'glyphicon glyphicon-file';
                if (lscache.get(node.sha)) {
                    node.class = "visisted";
                } else {
                    node.class = "not-visisted";
                }

                node.id = node.path;

            } else {
                node.nodes = node.children;
                node.icon = 'glyphicon glyphicon-folder-close';
                node.expandedIcon = 'glyphicon glyphicon-folder-open';
                node.selectable = false;
                node.state.expanded = true;
            }

            if (node.nodes && node.nodes.length > 0) {
                for (var i = 0; i < node.nodes.length; i++) {
                    fixNode(node.nodes[i]);
                }
            }

        }

        $.get(pathPrefix + '/files?expectedSha=' + headSha, function (files) {
            if (!checkVersions(files)) {
                return;
            }


            var data = files.data;
            for (var i = 0; i < data.length; i++) {
                fixNode(data[i]);
            }

            $('#tree').treeview({
                data: data,
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
        });
    }


    function loadComments() {
        $.get(pathPrefix + '/comments?expectedSha=' + headSha, function (result) {

            if (!checkVersions(result)) {
                return;
            }

            var data = result.comments;

            comments = {};
            for (var i in data) {
                var comment = data[i];
                var path = comment.path || '';
                if (!comments[path]) {
                    comments[path] = [];
                }
                comments[path].push(comment);
            }
            applyCommentsToTree();
            applyCommentsToEditors();
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

    var commentBlocks = {};

    function applyCommentsToEditors() {

        if (!comments) {
            return;
        }
        //clear currrently appled comments from system
        $('.comment').remove();

        function addComment(doc, line, comment) {
            commentBlocks[doc] = commentBlocks[doc] || {};
            var block = commentBlocks[doc][line];
            if (!block) {
                var elm = $('<div class="commentList" />');
                var widget = doc.addLineWidget(line, elm[0], { coverGutter: true, noHScroll: true, });
                commentBlocks[doc][line] = block = { elm: elm, widget: widget };
            }
            block.elm.append($('<div class="comment"><div class="header"><img src="' + comment.commenter.avatarUrl + '&s=40"> ' + comment.commenter.login + ' added a note <span class="timeago">' + comment.createdAt + '</span></div>' + marked(comment.body) + '</div>'));
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
                    if (comment.sourceLine > 0) {
                        addComment(sourceDoc, comment.sourceLine, comment);
                    }
                }
                if (targetDoc) {
                    if (comment.targetLine > 0) {

                        addComment(targetDoc, comment.targetLine, comment);
                    }
                }
            }
        }
        if (reloadEditors) {
            reloadEditors();
        }
    }
    //preiodically reload comments and reapply??? on loadPageMaybe???

    //called directly
    function loadPath(path, skipNavigation, refresh) {

        if (!skipNavigation) {
            var navPath = path;
            if (navPath) {
                navPath = '/files/' + navPath;
            }

            if (currentFilePath != path) {
                history.pushState({ path: path }, null, pathPrefix + navPath);
            } else {
                history.replaceState({ path: path }, null, pathPrefix + navPath);
            }
        }

        if (currentFilePath == path && !refresh) {
            return; //we are allready showing the page remain
        }
        currentFilePath = path;
        targetEditor = null;
        sourceEditor = null;
        commentBlocks = {};//reset the applied comments

        var targetElm = $('#file_diff');
        targetElm.html('<div class="loader">Loading ...</div>');
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
            //load the empty select a file screen... or maybe the general comments list!!!
            targetElm.html('<div class="loader">select a file to continue. </div>');
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

            applyCommentsToEditors();

        });
        loadComments();
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
        var splitter = $('#splitter');
        var offset = splitter.position().top;
        var winh = $(window).height() - offset;
        splitter.height(winh).trigger("resize");
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
};