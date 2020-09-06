import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import React, { Component } from 'react';
import Modal from 'react-bootstrap/Modal';
import Spinner from 'react-bootstrap/Spinner';
import Container from 'react-bootstrap/Container';
import Breadcrumb from 'react-bootstrap/Breadcrumb';
import AutoSizer from 'react-virtualized-auto-sizer';
import { FixedSizeGrid as Grid } from 'react-window';
import { extract, size, breakpoint } from './../utils';
import { MediaContainer } from './MediaContainer';
import { MediaViewer } from './MediaViewer';

export class Library extends Component {

    static displayName = Library.name;

    constructor(props) {
        super(props);
        this.rowCount = 1;
        this.columnCount = 1;
        this.mediaViewer = React.createRef();
        let path = "/" + extract("", props, "match", "params", 0) + window.location.search;
        this.state = {
            loading: false,
            status: "",
            path: path,
            items: [],
            query: "",
        };
    }

    componentDidMount() {
        this.list(this.state.path);
    }

    componentDidUpdate() {
    }

    list(path) {
        if (path) {
            this.setState({
                loading: true,
                status: "Requesting",
            });
            fetch("/library" + path)
            .then((response) => {
                this.setState({
                    status: "Loading",
                    items: []
                });
                return response.json();
            })
            .then((json) => {
                let items = Array.isArray(json) ? json : [json];
                this.setState({
                    loading: false,
                    path: path,
                    items: items
                });
                // now change the history!
                window.history.pushState({}, "", path);
                // pass on the source to the viewer if this is a file
                return Array.isArray(json) ? null : json;
            })
            .then((source) => {
                if (source !== null) {
                    this.view(source);
                }
            })
            .catch((error) => {
                this.setState({
                    loading: false,
                    status: "Error",
                    items: []
                });
            });
        }
    }

    search(query) {
        let recursive = true;
        let path = this.state.path.split('?')[0];
        let params = "?recursive=" + recursive;
        params += (query !== "") ? ("&query=" + query) : "";
        this.list(path + params);
    }

    open(source) {
        this.list(source.fullPath);
    }

    view(source, player = true) {
        this.mediaViewer.current.view(source, player);
    }

    highlight(source) {
        if (source !== null) {
        } else {
        }
    }

    cell({ columnIndex, rowIndex, style }) {
        let source = this.state.items[(rowIndex * this.columnCount) + columnIndex];
        if (source !== undefined) {
            return (
                <div className="p-1" style={style}>
                    <MediaContainer source={source} open={this.open.bind(this)} view={this.view.bind(this)} highlight={this.highlight.bind(this)} />
                </div>
            );
        } else {
            return (
                <div style={style}>
                </div>
            );
        }
    }

    files() {
        return this.state.items.reduce((count, item) =>{
            if ((item.type === "Video") || (item.type === "Audio") || (item.type === "Photo")) return (count + 1);
            return count;
        }, 0) + " Files";
    }

    render() {
        let location = "/";
        let url = "Library".concat(this.state.path);
        let path = url.split('?')[0];
        let params = url.split("?")[1];
        let search = (params !== undefined) && (params.indexOf("query=") !== -1);
        let folders = (search) ? (path.concat("/Search Results").split("/")) : (path.split("/"));
        return (
            <Container className="d-flex flex-column align-content-stretch" style={{flexGrow: 1, flexShrink: 1, flexBasis: 'auto'}}>
                <Breadcrumb className="">
                    {
                        folders.map((folder, index) => {
                            let link = location;
                            let active = false;
                            let last = (index === (folders.length - 1));
                            if (folder) {
                                if (index) {
                                    if (last) {
                                        if (search) {
                                            // it's the search results item, deactivate it
                                            link = "";
                                            active = true;
                                        } else {
                                            // don't add the trailing slash if this is a file
                                            location += folder;
                                            link = location;
                                        }
                                    } else {
                                        location += folder + "/";
                                        link = location;
                                    }
                                }
                                return (
                                    <Breadcrumb.Item key={"library-path-item-" + index} href="#" active={active} linkProps={{ link: link }} onClick={event => this.list(event.target.getAttribute("link"))} >{folder}</Breadcrumb.Item>
                                );
                            } else {
                                return null;
                            }
                        })
                    }
                </Breadcrumb>
                <div className="mb-3" style={{flexGrow: 1, flexShrink: 1, flexBasis: 'auto'}}>
                    <AutoSizer>
                        {({ height, width }) => {
                            let offset = 0;
                            let size = breakpoint();
                            switch (size) {
                                case 'xs':
                                    offset = 15;
                                    this.columnCount = 1;
                                    break;
                                case 'sm':
                                    offset = 15;
                                    this.columnCount = 1;
                                    break;
                                case 'md':
                                    offset = 8;
                                    this.columnCount = 2;
                                    break;
                                case 'lg':
                                    offset = 5;
                                    this.columnCount = 3;
                                    break;
                                case 'xl':
                                    offset = 4;
                                    this.columnCount = 4;
                                    break;
                                default:
                                    break;
                            }
                            let rowHeight = (width / this.columnCount);
                            let columnWidth = (width / this.columnCount) - offset;
                            this.rowCount = Math.ceil(this.state.items.length / this.columnCount);
                            return (
                                <Grid columnCount={this.columnCount} columnWidth={columnWidth} height={height} rowCount={this.rowCount} rowHeight={rowHeight} width={width}>
                                    {this.cell.bind(this)}
                                </Grid>
                            )}
                        }
                    </AutoSizer>
                    <MediaViewer ref={this.mediaViewer} />
                    <Modal show={this.state.loading} backdrop={false} animation={false} size="sm" aria-labelledby="contained-modal-title-vcenter" centered>
                        <Modal.Body className="shadow-sm">
                            <Container>
                                <Row>
                                    <Col className="text-center mt-4 mb-2">
                                        <Spinner className="loading-spinner" animation="border" variant="dark" />
                                    </Col>
                                </Row>
                                <Row>
                                    <Col className="text-center">
                                        <p className="font-weight-light text-uppercase" >{this.state.status}</p>
                                    </Col>
                                </Row>
                            </Container>
                        </Modal.Body>
                    </Modal>
                </div>
                <Breadcrumb>
                    <div style={{flexGrow: 1}}>{this.files()}</div>
                    <div>{size(this.state.items.reduce((sum, item) => sum + item.size, 0))}</div>
                </Breadcrumb>
            </Container>
        );
    }
}
