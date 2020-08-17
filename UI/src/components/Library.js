import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import React, { Component } from 'react';
import Modal from 'react-bootstrap/Modal';
import Spinner from 'react-bootstrap/Spinner';
import Container from 'react-bootstrap/Container';
import Breadcrumb from 'react-bootstrap/Breadcrumb';
import AutoSizer from 'react-virtualized-auto-sizer';
import { FixedSizeGrid as Grid } from 'react-window';
import { MediaContainer } from './MediaContainer';
import { extract, size, breakpoint } from './../utils';

export class Library extends Component {
    static displayName = Library.name;

    constructor(props) {
        super(props);
        this.rowCount = 1;
        this.columnCount = 1;
        let path = "/" + extract("", props, "match", "params", 0);
        this.state = {
            loading: false,
            status: "",
            path: path,
            items: [],
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
                status: "Requesting"
            });
            fetch("/list" + path)
            .then((response) => {
                this.setState({
                    status: "Loading",
                    items: []
                });
                return response.json();
            })
            .then((items) => {
                this.setState({
                    loading: false,
                    path: path,
                    items: items
                });
                // now change the history!
                window.history.pushState({}, "", path);
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

    open(source) {
        if (source.type === "Folder") {
            this.list(source.fullPath);
        } else if (source.type === "Video") {
            window.open("/stream/" + source.id + "/" + source.name, "_blank");
        }
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
                    <MediaContainer source={source} open={this.open.bind(this)} highlight={this.highlight.bind(this)} />
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
        let path = "/";
        return (
            <Container className="d-flex flex-column align-content-stretch" style={{flexGrow: 1, flexShrink: 1, flexBasis: 'auto'}}>
                <Breadcrumb className="">
                    {
                        "Library".concat(this.state.path).split("/").map((folder, index) => {
                            if (folder) {
                                if (index) {
                                    path += folder + "/";
                                }
                                return (
                                    <Breadcrumb.Item key={"library-path-item-" + index} href="#" linkProps={{ path: path }} onClick={event => this.list(event.target.getAttribute("path"))} >{folder}</Breadcrumb.Item>
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
                            let rowHeight = (width / this.columnCount);
                            switch (size) {
                                case 'xs':
                                    offset = 16;
                                    this.columnCount = 1;
                                    rowHeight = (width / this.columnCount) * 12 / 16;
                                    break;
                                case 'sm':
                                    offset = 8;
                                    this.columnCount = 2;
                                    rowHeight = (width / this.columnCount) * 14 / 16;
                                    break;
                                case 'md':
                                    offset = 5;
                                    this.columnCount = 3;
                                    rowHeight = (width / this.columnCount) * 15 / 16;
                                    break;
                                case 'lg':
                                    offset = 4;
                                    this.columnCount = 4;
                                    rowHeight = (width / this.columnCount) * 15 / 16;
                                    break;
                                case 'xl':
                                    offset = 4;
                                    this.columnCount = 4;
                                    rowHeight = (width / this.columnCount) * 15.7 / 16;
                                    break;
                                default:
                                    break;
                            }
                            this.rowCount = Math.ceil(this.state.items.length / this.columnCount);
                            let columnWidth = (width / this.columnCount) - offset;
                            return (
                                <Grid columnCount={this.columnCount} columnWidth={columnWidth} height={height} rowCount={this.rowCount} rowHeight={rowHeight} width={width}>
                                    {this.cell.bind(this)}
                                </Grid>
                            )}
                        }
                    </AutoSizer>
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
