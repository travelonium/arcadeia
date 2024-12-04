import { connect } from "react-redux";
import { toast } from 'react-toastify';
import React, { Component } from 'react';
import update from 'immutability-helper';
import NavMenu from './components/NavMenu';
import Library from './components/Library';
import Settings from './components/Settings';
import { Route, Routes } from 'react-router';
import { shorten, withRouter } from './utils';
import { setTheme } from './features/ui/slice';
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack';
import { HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr';

class App extends Component {
    static displayName = App.name;

    constructor(props) {
        super(props);
        this.library = React.createRef();
        this.scannerProgressToast = null;
        this.lastScannerProgressToastShowed = 0;
        this.state = {
            scanner: {
                scan: {
                    uuid: null,
                    title: null,
                    path: null,
                    item: null,
                    index: null,
                    total: null
                },
                update: {
                    uuid: null,
                    title: null,
                    item: null,
                    index: null,
                    total: null
                }
            }
        }
        // initialize the theme setting it to dark/light mode
        this.onSelectMode();
    }

    componentDidMount() {
        // update the mode dynamically based on the user’s system color scheme preference (light or dark)
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => this.onSelectMode(e.matches ? 'dark' : 'light'));
        // create the signalR connection and setup notifications
        this.setupSignalRConnection(this.setupNotifications.bind(this));
    }

    componentWillUnmount() {
        window.matchMedia('(prefers-color-scheme: dark)').removeEventListener('change', e => this.onSelectMode(this, e.matches ? 'dark' : 'light'));
    }

    onSelectMode(mode) {
        if (!mode) mode = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        this.props.dispatch(setTheme(mode));
    }

    setupNotifications(connection) {
        connection.on("Refresh", (path) => {
            this.library?.current?.refresh();
        });
        connection.on("ShowScanProgress", (uuid, title, path, item, index, total) => {
            const value = {
                uuid: uuid,
                title: title,
                path: path,
                item: item,
                index: index,
                total: total
            };
            this.setState(prevState => {
                return {
                    ...prevState,
                    scanner: update(prevState.scanner, {
                        $merge: {
                            scan: update(prevState.scanner.scan, {
                                $merge: value
                            })
                        }
                    })
                }
            }, () => {
                this.showScannerProgressToast(value);
            });
        });
        connection.on("ScanCancelled", (uuid, title, path) => {
            const value = {
                uuid: uuid,
                title: title,
                path: path,
                item: null,
                index: null,
                total: null
            };
            this.setState(prevState => {
                return {
                    ...prevState,
                    scanner: update(prevState.scanner, {
                        $merge: {
                            scan: update(prevState.scanner.scan, {
                                $merge: value
                            })
                        }
                    })
                }
            }, () => {
                this.showScannerCancelledToast(value);
            });
        });
        connection.on("ShowUpdateProgress", (uuid, title, item, index, total) => {
            const value = {
                uuid: uuid,
                title: title,
                item: item,
                index: index,
                total: total
            };
            this.setState(prevState => {
                return {
                    ...prevState,
                    scanner: update(prevState.scanner, {
                        $merge: {
                            update: update(prevState.scanner.update, {
                                $merge: value
                            })
                        }
                    })
                }
            }, () => {
                this.showScannerProgressToast(value);
            });
        });
        connection.on("UpdateCancelled", (uuid, title) => {
            const value = {
                uuid: uuid,
                title: title,
                item: null,
                index: null,
                total: null
            };
            this.setState(prevState => {
                return {
                    ...prevState,
                    scanner: update(prevState.scanner, {
                        $merge: {
                            update: update(prevState.scanner.update, {
                                $merge: value
                            })
                        }
                    })
                }
            }, () => {
                this.showScannerCancelledToast(value);
            });
        });
    }

    setupSignalRConnection(callback = undefined) {
        const connection = new HubConnectionBuilder()
                               .withUrl("/signalr", { transport: HttpTransportType.WebSockets })
                               .withAutomaticReconnect()
                               .withHubProtocol(new MessagePackHubProtocol())
                               .configureLogging(LogLevel.Information)
                               .build();
        connection.start()
        .then(() => {
            this.signalRConnection = connection;
            if (callback !== undefined) {
                callback(connection);
            }
        })
        .catch(error => console.error("Error while starting a SignalR connection: ", error));
    }

    showScannerProgressToast(state) {
        const { index, total, title, item } = state || {};
        if ([index, total, title, item].some(value => value == null)) return;
        const interval = 10; // the throttling interval in ms
        const now = Date.now();
        if ((now - this.lastScannerProgressToastShowed < interval) && (index > 0) && ((index + 1) < total)) return;
        const progress = (index + 1) / total;
        const render = this.renderScannerProgressToast(`${title}...`, item);
        if (!this.scannerProgressToast) {
            this.scannerProgressToast = toast.info(render,
            {
                theme: (this.props.ui.theme === 'dark') ? 'dark' : 'light',
                progress: progress,
                icon: <div className="Toastify__spinner"></div>,
                type: (progress === 1) ? 'success' : 'info',
            });
        } else {
            toast.update(this.scannerProgressToast, {
                render: render,
                progress: progress,
                type: (progress === 1) ? 'success' : 'info',
            });
        }
        if (progress === 1) {
            if (toast.isActive(null)) toast.dismiss(this.scannerProgressToast);
            this.scannerProgressToast = null;
        }
        this.lastScannerProgressToastShowed = now;
    }

    showScannerCancelledToast(state) {
        const { title } = state || {};
        if ([title].some(value => value == null)) return;
        const render = this.renderScannerProgressToast(`${title}`);
        if (this.scannerProgressToast) {
            toast.update(this.scannerProgressToast, {
                progress: null,
                render: render,
                type: 'warning'
            });
        }
        this.scannerProgressToast = null;
    }

    renderScannerProgressToast(title, subtitle) {
        return (
            <>
                <div className="mb-1">
                    <strong>{title}</strong>
                </div>
            {
                subtitle ? <small className="scanner-progress-subtitle">{shorten(subtitle, 100)}</small> : <></>
            }
            </>
        );
    }

    render() {
        return (
            <>
                <NavMenu library={this.props.library} />
                <Routes>
                    <Route path="/settings/*" element={<Settings />} />
                    <Route exact path='/*' element={<Library ref={this.library} forwardedRef={this.library} />} />
                </Routes>
            </>
        );
    }
}

const mapStateToProps = (state) => ({
    ui: {
        theme: state.ui.theme,
    }
});

export default connect(mapStateToProps, null, null)(withRouter(App));

