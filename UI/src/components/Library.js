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
import { extract } from './../utils';

/*
<Col key={"media-container-column-index-" + index}>
    <MediaContainer source={this.state.items[index]} open={this.open} />
</Col>
*/

export class Library extends Component {
    static displayName = Library.name;

    constructor(props) {
        super(props);
        this.open = this.open.bind(this);
        let path = "/" + extract("", props, "match", "params", 0);
        this.state = {
            loading: false,
            status: "",
            path: path,
            items: []
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
                    status: "Loading"
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

    cell({ columnIndex, rowIndex, style }) {
        let source = this.state.items[(rowIndex * 4) + columnIndex];
        if (source !== undefined) {
            return (
                <div className="p-1" style={style}>
                    <MediaContainer source={source} open={this.open} />
                </div>
            );
        } else {
            return (
                <div style={style}>
                </div>
            );
        }
    }

    render() {
        let path = "/";
        return (
            <div className="d-flex flex-column align-content-stretch h-100">
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
                <div style={{"flex-grow": "1"}}>
                    <AutoSizer>
                        {({ height, width }) => (
                            <Grid columnCount={4} columnWidth={(width / 4) - 5} height={height} rowCount={this.state.items.length / 4} rowHeight={250} width={width}>
                                {this.cell.bind(this)}
                            </Grid>
                        )}
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
            </div>
        );
    }
}
