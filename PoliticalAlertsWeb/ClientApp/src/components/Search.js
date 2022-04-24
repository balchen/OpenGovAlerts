import React, { Component } from 'react';
import EditableLabel from 'react-editable-label';
import moment from 'moment';

export class Search extends Component {
    displayName = Search.name;

    constructor(props) {
        super(props);

        this.state = {
            model: {}, loading: true
        };

        this.updateModel = this.updateModel.bind(this);
        this.renderModel = this.renderModel.bind(this);

        fetch("api/Member/getSearch?id=" + this.props.id)
            .then(response => response.json())
            .then(data => {
                this.setState({ model: data, loading: false });
            });
    }

    updateModel() {
        fetch("api/Member/updateSearch?id=" + this.props.id, {
                method: "POST",
                body: JSON.stringify(this.state.model),
                headers: {
                    'Content-Type': 'application/json'
                    // 'Content-Type': 'application/x-www-form-urlencoded',
                }
            })
            .then(response => response.json())
            .then(data => {
                this.setState({ model: data, loading: false });
            });
    }

    renderModel(model) {
        var sources = model.sources.map(s => <tr key={s.source.id}><td><input type="checkbox" onChange={e =>
        {
            this.setState(state => {
                    var source = state.model.sources.find(searchSource => searchSource.source.id === s.source.id);
                    source.selected = !source.selected;
                    console.log(source);
                },
                () => this.forceUpdate() // honestly don't know why I need to forceUpdate here and not in the onChange/save below
            );
        }} checked={s.selected} /></td><td>{s.source.name}</td></tr>);

        var matches = model.recentMatches.map(m => <tr key={m.id}><td><a href={m.meeting.url}>{moment(m.meeting.date).format("DD.MM.YYYY")}</a></td><td><a href={m.meeting.url}>{m.meeting.boardName}</a></td><td><a href={m.meeting.url}>{m.meeting.source.name}</a></td><td><a href={m.meeting.url}>{m.meeting.title}</a></td></tr>);

        return <div>
            <h1><EditableLabel initialValue={model.search.name} save={v => this.setState(state => state.model.search.name = v)} /></h1>
            <div>Søkeord: <input type="text" onChange={e =>
            {
                var phrase = e.target.value;
                this.setState(state => state.model.search.phrase = phrase);
            }} value={model.search.phrase} /></div>
            <table><tbody>{sources}</tbody></table>
            <button onClick={this.updateModel}>Lagre</button>
            <h2>Siste saker</h2>
            <table className="listing"><tbody>{matches}</tbody></table>
        </div>;
    }

    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : this.renderModel(this.state.model);

        return contents;
    }
}
