/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

import { withRouter } from './utils';
import { connect } from "react-redux";
import { toast } from 'react-toastify';
import React, { Component } from 'react';
import update from 'immutability-helper';
import NavMenu from './components/NavMenu';
import Library from './components/Library';
import Settings from './components/Settings';
import { Route, Routes } from 'react-router';
import { setTheme } from './features/ui/slice';
import ProgressToast from './components/ProgressToast';
import { readSettings } from './features/settings/slice';
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack';
import { HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr';

class App extends Component {
    static displayName = App.name;

    constructor(props) {
        super(props);
        this.signalrConnection = null;
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
        // read the settings
        this.props.dispatch(readSettings());
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
        this.signalrConnection = connection;
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
        const render = <ProgressToast title={title} subtitle={item} />;
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
        const render = <ProgressToast title={title} />;
        if (this.scannerProgressToast) {
            toast.update(this.scannerProgressToast, {
                progress: null,
                render: render,
                type: 'warning'
            });
        }
        this.scannerProgressToast = null;
    }

    render() {
        return (
            <>
                <NavMenu library={this.library} />
                <Routes>
                    <Route path="/settings/*" element={<Settings />} />
                    <Route exact path='/*' element={<Library ref={this.library} forwardedRef={this.library} signalRConnection={this.signalrConnection} />} />
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

