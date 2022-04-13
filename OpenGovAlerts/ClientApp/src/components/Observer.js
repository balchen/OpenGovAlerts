import React, { Component } from 'react';
import { LinkContainer } from 'react-router-bootstrap';
import { Button } from 'react-bootstrap';

export class Observer extends Component {
    static renderModel(model) {
        var searches = model.observer.searches != null ? model.observer.searches.map(s => <LinkContainer key={s.id} to={'/Observer/' + model.observer.id + '/Search/' + s.id}><Button>{s.name}</Button></LinkContainer>) : null;
        return <div>
            <h1>{model.observer.name}</h1>
            {searches}
        </div>;
    }

    displayName = Observer.name;

    constructor(props) {
        super(props);

        this.state = {
            model: {}, loading: true
        };

        fetch("api/Member/getObserver?id=" + this.props.id)
            .then(response => response.json())
            .then(data => {
                this.setState({ model: data, loading: false });
            });
    }

    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : Observer.renderModel(this.state.model);

        return contents;
    }
}
